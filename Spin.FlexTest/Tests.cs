using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Spin.Pillars;
using System.Reflection;
using Spin.Pillars.Logging;

namespace Spin.FlexTest
{
  public class Tests : List<Test>
  {
    public static bool IsRunning { get; private set; }
    public LogScope Log { get; }

    private Dictionary<String, Test> _index;


    public static Tests FromAssembly() =>
      new Tests(
        Assembly.GetCallingAssembly().GetTypes()
        .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static))
        .Select(x => new { Method = x, Attribute = x.GetCustomAttribute<TestAttribute>() ?? x.GetCustomAttribute<ClassTestAttribute>() })
        .Where(x => x.Attribute != null)
        .Select(x => new Test(x.Method)));

    public static Tests Load(params string[] assemblyNames) => Load(assemblyNames.Select(x => Assembly.Load(x)));

    private static IEnumerable<Assembly> GetReferencedAssemblies() => Assembly.GetExecutingAssembly().Traverse(x => x.GetReferencedAssemblies().Select(y => Assembly.Load(y)));
    private static IEnumerable<Assembly> GetReferencedAssemblies(IEnumerable<string> assemblyNames) => Assembly.GetExecutingAssembly().GetReferencedAssemblies().Where(x => assemblyNames.Contains(x.Name)).Select(x => Assembly.Load(x));

    public static Tests Load(params Assembly[] assemblies) => Load((IEnumerable<Assembly>)assemblies);
    public static Tests Load(IEnumerable<Assembly> assemblies) =>
      new Tests(
        assemblies.SelectMany(y => y.GetTypes()
          .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static))
          .Select(x => new { Method = x, Attribute = x.GetCustomAttribute<TestAttribute>() })
          .Where(x => x.Attribute != null)
          .Select(x => new Test(x.Method))));


    public Tests(IEnumerable<Test> source) : base(source)
    {
      Log = Pillars.Logging.Log.Start("Tests");
      foreach (var test in this)
        test.PopulateDependencies(this);
    }

    public Dictionary<Type, Object> DependencyCache { get; } = new Dictionary<Type, object>();

    public void AddDependency(object dependency) => DependencyCache.Add(dependency.GetType(), dependency);

    public void Run(Func<Test, bool> predicate = null)
    {
      IsRunning = true;

      Log.Capture("Tests", () =>
      {
        foreach (var test in this.OrderByDependency().Where(predicate ?? (x => true)))
          test.Run(DependencyCache);
      });

      DependencyCache.Clear();
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
        throw new Exception($"Duplicate test name '{duplicate.Key}' for methods: {String.Join(",", duplicate.Select(x => x.Target.Name))}");
      }

      foreach (var test in this)
        _index.Add(test.Name, test);
    }

    public IEnumerable<Test> OrderByDependency()
    {
      var cache = new HashSet<Type>()
      {
        typeof(Test),
      };

      foreach (var dependency in DependencyCache.Keys)
        cache.Add(dependency);

      var tests = new List<Test>(this);
      var completed = new HashSet<Test>();

      bool any = false;
      do
      {
        any = false;
        foreach (var test in tests.CopyOf().Where(x => x.HasDependencies(cache, completed)))
        {
          if (test.ReturnsObject != null)
            cache.Add(test.ReturnsObject);
          yield return test;
          tests.Remove(test);
          completed.Add(test);
          if (!any)
            any = true;
        }
      } while (any);

      if (tests.Count > 0)
      {
        var missing = String.Join(", ", tests.SelectMany(x => x.GetMissingDependencies(cache)).Distinct().Select(x => x.FullName));
        throw new Exception($"Objects are required but were not provided by test methods: {missing}");
      }
    }
  }
}
