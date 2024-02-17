using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;

using Spin.Pillars.Hierarchy;
using Spin.Pillars.Logging;
using System.Runtime.InteropServices;
using System.Security;

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
  public string Variation { get; set; }
  public int WarmupIterations { get; set; } = 1;
  public int TestIterations { get; set; } = 3;

  public TestFixture Fixture { get; set; }
  public Action Action { get; }
  public LogScope Log { get; }

  //Results
  public BenchmarkResults Results { get; private set; }
  public Stopwatch Timer { get; } = new();

  public Benchmark(BenchmarkAttribute attribute, MethodInfo target, LogScope log)
  {
    attribute.Intialize(target);

    Name = attribute.Name;
    if (!String.IsNullOrWhiteSpace(attribute.Category))
      Category = Path.Parse(attribute.Category, '\\');
    Variation = attribute.Variation;
    WarmupIterations = attribute.WarmupIterations;
    TestIterations = attribute.TestIterations;
    Log = log;
    Action = () => target.Invoke(Fixture, Array.Empty<Object>());
  }

  public void Execute() => Execute(Action);

  [DllImport("Kernel32.dll"), SuppressUnmanagedCodeSecurity]
  public static extern int GetCurrentProcessorNumber();

  protected virtual void Execute(Action action)
  {
    if (Fixture is not null)
      Fixture.ExecutingBenchmark = this;

    var getThreads = () => Process.GetCurrentProcess().Threads.OfType<ProcessThread>().Where(x => x.PriorityLevel == ThreadPriorityLevel.AboveNormal);
    var threads = getThreads().ToList();

    var log = Log.Start(Name);
    Results = new BenchmarkResults();
    bool useBackgroundThread = true;
    //Process.GetCurrentProcess().ProcessorAffinity = 7;
    if (useBackgroundThread)
    {
      var thread = new System.Threading.Thread(x => Execute(action, log));
      thread.Priority = System.Threading.ThreadPriority.AboveNormal;
      thread.Start();
      Debug.WriteLine($"After: {Process.GetCurrentProcess().Threads.Count}");

      //Try to use the same physical core each time.
      var newThreds = getThreads().Except(threads).ToList();
      if (newThreds.Count > 1)
        throw new Exception(newThreds.Count.ToString());
      //newThreds.First().IdealProcessor = 0; //This doesn't do anything.
      //8,1,7,3,6....13,14 (1-based)
      //Used HWINFO to hard-code my fastest core.
      newThreds.First().ProcessorAffinity = 64;  

      thread.Join();
    }
    else
      Execute(action, log);

    if (Fixture is not null)
      Fixture.ExecutingBenchmark = null;
  }

  private void Execute(Action action, LogScope log)
  {
    Debug.WriteLine($"Current CPU: {GetCurrentProcessorNumber()}");
    try
    {
      for (int i = 0; i < WarmupIterations; i++)
      {
        OnIterationStarted(true);
        Timer.Restart();
        action();
        var elapsed = Timer.Elapsed;
        Results.Add(elapsed, true);
        OnIterationCompleted(elapsed);
        Debug.WriteLine($"Current CPU: {GetCurrentProcessorNumber()}");
      }
      for (int i = 0; i < TestIterations; i++)
      {
        OnIterationStarted(false);
        Timer.Restart();
        action();
        var elapsed = Timer.Elapsed;
        Results.Add(elapsed, false);
        OnIterationCompleted(elapsed);
        Debug.WriteLine($"Current CPU: {GetCurrentProcessorNumber()}");
      }
      Results.TestDuration = log.Finish().Elapsed;
      Results.Succeeded = true;
      log.Write("Result: {duration}", Results.Average);
    }
    catch (TargetInvocationException ex)
    {
      Results.TestDuration = log.Failed(ex.InnerException).Elapsed;
      Results.Succeeded = false;
      Results.Error = ex.InnerException.Message;
    }
    catch (Exception ex)
    {
      Results.TestDuration = log.Failed(ex).Elapsed;
      Results.Succeeded = false;
      Results.Error = ex.Message;
    }
  }


  #region IterationStartedEventArgs Subclass
  public class IterationStartedEventArgs : EventArgs
  {
    public bool IsWarmup { get; private set; }
    internal IterationStartedEventArgs(bool isWarmup) => IsWarmup = isWarmup;
  }
  #endregion

  public event global::System.EventHandler<IterationStartedEventArgs> IterationStarted;
  protected void OnIterationStarted(bool isWarmup) => OnIterationStarted(new IterationStartedEventArgs(isWarmup));
  protected virtual void OnIterationStarted(IterationStartedEventArgs e) => IterationStarted?.Invoke(this, e);


  #region IterationCompletedEventArgs Subclass
  public class IterationCompletedEventArgs : EventArgs
  {
    public TimeSpan TimeSpan { get; private set; }
    internal IterationCompletedEventArgs(TimeSpan timeSpan) => TimeSpan = timeSpan;
  }
  #endregion

  public event global::System.EventHandler<IterationCompletedEventArgs> IterationCompleted;
  protected void OnIterationCompleted(TimeSpan timeSpan) => OnIterationCompleted(new IterationCompletedEventArgs(timeSpan));
  protected virtual void OnIterationCompleted(IterationCompletedEventArgs e) => IterationCompleted?.Invoke(this, e);


}
