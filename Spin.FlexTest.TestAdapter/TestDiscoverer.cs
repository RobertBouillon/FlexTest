using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Spin.FlexTest;
using System.Reflection;

//using Spin.FlexTest;

namespace Spin.FlexText.TestAdapter
{
  [FileExtension(".dll")]
  [DefaultExecutorUri(ExecutorUri)]
  public class TestDiscoverer : ITestDiscoverer
  {
    public const string ExecutorUri = "executor://flextest/v1";
    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
      foreach (var source in sources)
        foreach (var test in DiscoverTests(source))
          discoverySink.SendTestCase(CreateTestCase(test, source));
    }

    static internal IEnumerable<Test> DiscoverTests(string source)
    {
      var harness = Assembly.LoadFile(source).GetTypes().SingleOrDefault(x => x.BaseType == typeof(FlexTestHarness));
      if (harness is null)
        return Enumerable.Empty<Test>();
      return (Activator.CreateInstance(harness) as FlexTestHarness).DiscoverTests();
    }

    private TestCase CreateTestCase(Test test, string source) => new TestCase(
      test.TestExplorerName ?? test.Name,
      new(ExecutorUri),
      source)
    {
      LineNumber = test.SourceLineNumber,
      CodeFilePath = test.SourceFile
    };
  }
}