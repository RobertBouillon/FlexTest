using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Spin.Pillars.Logging;

namespace Spin.FlexTest;
public class Benchmarks : List<Benchmark>
{
  public static bool IsRunning { get; private set; }
  public LogScope Log { get; }

  private Dictionary<String, Test> _index;

  public static Benchmarks FromAssembly(LogScope log) => Load(log, Assembly.GetCallingAssembly());
  public static Benchmarks Load(LogScope log, params string[] assemblyNames) => Load(log, assemblyNames.Select(x => Assembly.Load(x)));
  public static Benchmarks Load(LogScope log, params Assembly[] assemblies) => Load(log, (IEnumerable<Assembly>)assemblies);
  public static Benchmarks Load(IEnumerable<string> assemblyNames) => Load(Pillars.Logging.Log.DefaultScope, assemblyNames.Select(x => Assembly.Load(x)));
  public static Benchmarks Load(params string[] assemblyNames) => Load((IEnumerable<string>)assemblyNames);
  public static Benchmarks Load(LogScope log, IEnumerable<Assembly> assemblies) //=> new(Test.Gather(log, assemblies));
  {
    var fixtureBenchmarks = assemblies
      .SelectMany(TestFixture.Gather)
      .Select(Activator.CreateInstance)
      .Cast<TestFixture>()
      .SelectMany(x => x.GatherBenchmarks(log));

    var staticBenchmarks = assemblies
      .SelectMany(x => x.GetTypes())
      .Where(x => !TestFixture.IsTestFixture(x))
      .SelectMany(x => Benchmark.Gather(log, x));

    return new Benchmarks(fixtureBenchmarks.Concat(staticBenchmarks));
  }

  #region Constructors
  public Benchmarks(IEnumerable<Benchmark> source) : base(source) => Log = Pillars.Logging.Log.Start("Benchmarks");
  #endregion

  #region Public Methods

  public void Run(Func<Benchmark, bool> predicate = null)
  {
    IsRunning = true;

    foreach (var benchmark in this.Where(predicate ?? (x => true)))
      benchmark.Execute();

    IsRunning = false;
  }

  public Benchmark this[string name] => this.FirstOrDefault(x=>x.Name == name);
  #endregion
}
