using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Spin.FlexTest;

namespace Spin.FlexText.TestAdapter
{
  [ExtensionUri(TestDiscoverer.ExecutorUri)]
  public class TestExecutor : ITestExecutor
  {
    public void Cancel() { }

    public void RunTests(IEnumerable<TestCase> testCases, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
      if (testCases is null)
        return;

      var index = testCases.ToDictionary(x => x.FullyQualifiedName);

      var tests = testCases
        .Select(x => x.Source)
        .Distinct()
        .SelectMany(TestDiscoverer.DiscoverTests)
        .Select(x=>(TestCase: index.GetValueOrDefault(x.TestExplorerName), Test: x))
        .Where(x => x.TestCase is not null)
        .ToList();

      foreach (var test in tests)
        RunTest(test.Test, test.TestCase, frameworkHandle);
    }

    public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
      if (sources is null)
        return;

      //Not implemented. Need test cases, so need to recreate the objects using methods in the Discoverer. Not sure it's needed.
      frameworkHandle.SendMessage(Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.TestMessageLevel.Error, "Not Implemented");
    }

    private void RunTest(Test test, TestCase testCase, IFrameworkHandle handle)
    {
      var startTime = DateTime.Now;
      handle.RecordStart(testCase);
      test.Execute();
      var outcome = test.Succeeded ? TestOutcome.Passed : TestOutcome.Failed;
      handle.RecordEnd(testCase, outcome);
      handle.RecordResult(new TestResult(testCase)
      {
        ComputerName = Environment.MachineName,
        Duration = test.Duration,
        EndTime = DateTime.Now,
        ErrorMessage = test.Error,
        Outcome = outcome,
        StartTime = startTime
      });
    }
  }
}