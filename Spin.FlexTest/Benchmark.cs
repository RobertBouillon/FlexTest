using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;

using Spin.Pillars.Hierarchy;
using Spin.Pillars.Logging;

namespace Spin.FlexTest;
public class Benchmark
{
  public static IEnumerable<Benchmark> Gather(LogScope log, params Type[] types) => Gather(log, types.Cast<Type>());
  public static IEnumerable<Benchmark> Gather(LogScope log, IEnumerable<Type> types) => types
    .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
    .Select(x => (BenchmarkAttribute: x.GetCustomAttribute<BenchmarkAttribute>(), Method: x))
    .Where(x => x.BenchmarkAttribute is not null && x.Method.ReturnType == typeof(void))
    .Select(x => new Benchmark(x.BenchmarkAttribute, x.Method, log));

  public static IEnumerable<Benchmark> Gather(LogScope log, params Assembly[] assemblies) => Gather(log, (IEnumerable<Assembly>)assemblies);
  public static IEnumerable<Benchmark> Gather(LogScope log, IEnumerable<Assembly> assemblies) => Gather(log, assemblies.SelectMany(x => x.GetTypes()));

  //Configuration
  public string Name { get; set; }
  public Path Category { get; set; }
  public int WarmupIterations { get; set; } = 1;
  public int TestIterations { get; set; } = 3;

  public TestFixture Fixture { get; set; }
  public Action Action { get; }
  public LogScope Log { get; }

  //Results
  public List<TimeSpan> Results { get; protected set; } = new();
  public List<TimeSpan> WarmupResults { get; protected set; } = new();
  public TimeSpan OverallResult => TimeSpan.FromTicks((long)Results.Select(x => x.Ticks).Average());
  public TimeSpan Duration { get; protected set; }
  public bool Succeeded { get; protected set; }
  public string Error { get; protected set; }
  public Dictionary<String, TestMetric> Metrics { get; set; } = new Dictionary<string, TestMetric>();
  public Stopwatch Timer { get; } = new();

  public Benchmark(BenchmarkAttribute attribute, MethodInfo target, LogScope log)
  {
    attribute.Intialize(target);

    Name = attribute.Name;
    if (!String.IsNullOrWhiteSpace(attribute.Category))
      Category = Path.Parse(attribute.Category, '\\');
    WarmupIterations = attribute.WarmupIterations;
    TestIterations = attribute.TestIterations;
    Log = log;
    Action = () => target.Invoke(Fixture, Array.Empty<Object>());
  }

  public void Execute() => Execute(Action);

  protected virtual void Execute(Action action)
  {
    if (Fixture is not null)
      Fixture.ExecutingBenchmark = this;

    var log = Log.Start(Name);
    try
    {
      for (int i = 0; i < WarmupIterations; i++)
      {
        Timer.Restart();
        action();
        WarmupResults.Add(Timer.Elapsed);
      }
      for (int i = 0; i < TestIterations; i++)
      {
        Timer.Restart();
        action();
        Results.Add(Timer.Elapsed);
      }
      Duration = log.Finish().Elapsed;
      log.Write("Result: {duration}", OverallResult);
    }
    catch (TargetInvocationException ex)
    {
      Duration = log.Failed(ex.InnerException).Elapsed;
      Succeeded = false;
      Error = ex.InnerException.Message;
    }
    catch (Exception ex)
    {
      Duration = log.Failed(ex).Elapsed;
      Succeeded = false;
      Error = ex.Message;
    }

    if (Fixture is not null)
      Fixture.ExecutingBenchmark = null;
  }
}
