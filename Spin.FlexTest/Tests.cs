using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Spin.Pillars.Logging;

namespace Spin.FlexTest;

public class Tests : List<Test>
{
  public static bool IsRunning { get; private set; }
  public LogScope Log { get; }

  private Dictionary<String, Test> _index;

  public static Tests FromAssembly(LogScope log) => Load(log, Assembly.GetCallingAssembly());
  public static Tests Load(LogScope log, params string[] assemblyNames) => Load(log, assemblyNames.Select(x => Assembly.Load(x)));
  public static Tests Load(LogScope log, params Assembly[] assemblies) => Load(log, (IEnumerable<Assembly>)assemblies);
  public static Tests Load(params string[] assemblyNames) => Load(Pillars.Logging.Log.DefaultScope, assemblyNames.Select(x => Assembly.Load(x)));
  public static Tests Load(LogScope log, IEnumerable<Assembly> assemblies) //=> new(Test.Gather(log, assemblies));
  {
    var fixtureTests = assemblies
      .SelectMany(TestFixture.Gather)
      .Select(Activator.CreateInstance)
      .Cast<TestFixture>()
      .SelectMany(x => x.GatherTests(log));

    var staticTests = assemblies
      .SelectMany(x => x.GetTypes())
      .Where(x => !TestFixture.IsTestFixture(x))
      .SelectMany(x => Test.Gather(log, x));

    return new Tests(fixtureTests.Concat(staticTests));
  }

  public static void Execute(params string[] filter)
  {
    var testNames = new HashSet<string>(filter).Distinct();
    Execute(x => testNames.Contains(x.Name));
  }

  public static void Execute(Func<Test, bool> predicate = null)
  {
    var tests = Load(Pillars.Logging.Log.DefaultScope, GetTestableAssemblies());
    tests.Run(predicate);
  }

  private static IEnumerable<Assembly> GetTestableAssemblies()
  {
    var flextest = typeof(Tests).Assembly.GetName().FullName;
    return GetReferencedAssemblies().Where(x => x.GetReferencedAssemblies().Any(x => x.FullName == flextest));
  }
  
  private static IEnumerable<Assembly> GetReferencedAssemblies() => Assembly.GetEntryAssembly().GetReferencedAssemblies().Select(y => Assembly.Load(y)).Concat(Assembly.GetEntryAssembly());
  private static IEnumerable<Assembly> GetReferencedAssemblies(IEnumerable<string> assemblyNames) => Assembly.GetCallingAssembly().GetReferencedAssemblies().Where(x => assemblyNames.Contains(x.Name)).Select(x => Assembly.Load(x));

  public Tests(IEnumerable<Test> source) : base(source) =>
    Log = Pillars.Logging.Log.Start("Tests");

  public void Run(Func<Test, bool> predicate = null)
  {
    IsRunning = true;

    Log.Capture("Tests", () =>
    {
      foreach (var test in this.Where(predicate ?? (x => true)))
        test.Execute();
    });

    IsRunning = false;
  }

  public Test this[string name]
  {
    get
    {
      if (_index == null || _index.Count != Count)
        BuildIndex();
      return _index[name];
    }
  }

  public bool TryGetTest(string name, out Test test) => _index.TryGetValue(name, out test);

  private void BuildIndex()
  {
    _index = new Dictionary<string, Test>();
    if (this.Select(x => x.Name).Distinct().Count() < Count)
    {
      var duplicate = this.GroupBy(x => x.Name).Where(x => x.Count() > 1).First();
      throw new Exception($"Duplicate test name '{duplicate.Key}' for methods: {String.Join(",", duplicate.Select(x => x.Name))}");
    }

    foreach (var test in this)
      _index.Add(test.Name, test);
  }
}
