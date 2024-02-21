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
using System.Threading;
using System.Runtime;

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

  private Thread _thread;
  private volatile bool _stopBenchmark;
  private CancellationTokenSource _cancelBenchmarkThread;
  private bool _isBenchmarkRunning;

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
    if (_isBenchmarkRunning)
      throw new InvalidOperationException("Benchmark is already running");

    if (Fixture is not null)
      Fixture.ExecutingBenchmark = this;

    var getThreads = () => Process.GetCurrentProcess().Threads.OfType<ProcessThread>().Where(x => x.PriorityLevel == ThreadPriorityLevel.AboveNormal);
    var threads = getThreads().ToList();

    var log = Log.Start(Name);
    Results = new BenchmarkResults();

    _stopBenchmark = false;
    _cancelBenchmarkThread = new();

    _thread = new Thread(x => Execute(action, log));
    _thread.Priority = ThreadPriority.AboveNormal;
    _thread.Start();

    _isBenchmarkRunning = true;
    //Try to use the same physical core each time.
    var newThreds = getThreads().Except(threads).ToList();
    if (newThreds.Count > 1)
      throw new Exception(newThreds.Count.ToString());
    //newThreds.First().IdealProcessor = 0; //This doesn't do anything.
    //8,1,7,3,6....13,14 (1-based)
    //Used HWINFO to hard-code my fastest core.
    newThreds.First().ProcessorAffinity = 64;
  }

  public bool TryCancel()
  {
    if (!_isBenchmarkRunning)
      throw new InvalidOperationException("Benchmark is not running");

    _stopBenchmark = true;
    //Can't abort =( LOOK AT HOW THEY MASSACRED MY BOY (ControlledExecution)
    if (!_thread.Join(TimeSpan.FromSeconds(2)))
    {
      _cancelBenchmarkThread.Cancel();
      if (!_thread.Join(TimeSpan.FromSeconds(2)))
        return false;
    }
    return true;
  }

  private void Execute(Action action, LogScope log)
  {
    try
    {
      for (int i = 0; i < WarmupIterations; i++)
      {
        if (_stopBenchmark)
          break;
        OnIterationStarted(true);
        Timer.Restart();
        ControlledExecution.Run(action, _cancelBenchmarkThread.Token);
        var elapsed = Timer.Elapsed;
        Results.Add(elapsed, true);
        OnIterationCompleted(elapsed);
      }
      for (int i = 0; i < TestIterations; i++)
      {
        if (_stopBenchmark)
          break;
        OnIterationStarted(false);
        Timer.Restart();
        ControlledExecution.Run(action, _cancelBenchmarkThread.Token);
        var elapsed = Timer.Elapsed;
        Results.Add(elapsed, false);
        OnIterationCompleted(elapsed);
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
    finally
    {
      if (Fixture is not null)
        Fixture.ExecutingBenchmark = null;
      OnCompleted();
      _isBenchmarkRunning = false;
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


  public event EventHandler Completed;
  protected void OnCompleted() => OnCompleted(EventArgs.Empty);
  protected virtual void OnCompleted(EventArgs e) => Completed?.Invoke(this, e);



}
