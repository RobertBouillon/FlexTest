using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Spin.Pillars;
using System.Reflection;
using Spin.Pillars.Logging;
using System.Threading.Tasks;

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
        .SelectMany(x => x.GetMethods(BindingFlags.Public))
        .Select(x => new { Method = x, Attribute = x.GetCustomAttribute<TestAttribute>() ?? x.GetCustomAttribute<ClassTestAttribute>() })
        .Where(x => x.Attribute != null)
        .Select(x => new Test(x.Method)));

    public static Tests Load(params string[] assemblyNames) => Load(assemblyNames.Select(x => Assembly.Load(x)));

    private static IEnumerable<Assembly> GetReferencedAssemblies() => Assembly.GetExecutingAssembly().Traverse(x => x.GetReferencedAssemblies().Select(y => Assembly.Load(y)));
    private static IEnumerable<Assembly> GetReferencedAssemblies(IEnumerable<string> assemblyNames) => Assembly.GetExecutingAssembly().GetReferencedAssemblies().Where(x => assemblyNames.Contains(x.Name)).Select(x => Assembly.Load(x));

    public static Tests Load(params Assembly[] assemblies) => Load((IEnumerable<Assembly>)assemblies);
    public static Tests Load(IEnumerable<Assembly> assemblies) =>
      new Tests(assemblies.SelectMany(
        y => y.GetTypes()
          .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
          .Select(x => new { Method = x, Attribute = x.GetCustomAttribute<TestAttribute>() })
          .Where(x => x.Attribute != null)
          .Select(x => new Test(x.Method))));

    //{
    //  var foo = assemblies.SelectMany(
    //    y => y.GetTypes()
    //      .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
    //      .Select(x => new { Method = x, Attribute = x.GetCustomAttribute<TestAttribute>() })
    //      .Where(x => x.Attribute != null)
    //      .Select(x => new Test(x.Method)))
    //      .ToList();

    //  throw new NotImplementedException();
    //}

    public Tests(IEnumerable<Test> source) : base(source)
    {
      Log = Pillars.Logging.Log.Start("Tests");
    }

    //public Dictionary<Type, Object> DependencyCache { get; } = new Dictionary<Type, object>();

    //public void AddDependency(object dependency) => DependencyCache.Add(dependency.GetType(), dependency);

    public void Run(Func<Test, bool> predicate = null)
    {
      IsRunning = true;

      Log.Capture("Tests", () =>
      {
        foreach (var test in this.Where(predicate ?? (x => true)))
          test.Execute();
        //Parallel.ForEach(this.Where(predicate ?? (x => true)), test => test.Execute());
      });

      //DependencyCache.Clear();
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
  }
}
