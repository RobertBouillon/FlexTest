using Spin.Pillars.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spin.FlexTest
{
  internal class PerformanceTest : Test
  {
    private int Warmup { get; set; } = 1;
    private int Iterations { get; set; } = 3;
    public TimeSpan Benchmark { get; private set; }
    public List<TimeSpan> Results { get; private set; }

    internal PerformanceTest(TestAttribute attribute, MethodInfo target, LogScope parentLog) : base(attribute, target, parentLog) { }

    protected override void Execute(Action action)
    {
      var last = TimeSpan.Zero;
      Results = new List<TimeSpan>(Iterations);
      Log.Start();
      try
      {
        for (int i = 0; i < Warmup; i++)
          action();

        last = Log.Elapsed;
        for (int i = 0; i < Iterations; i++)
        {
          action();
          Results.Add(last = Log.Elapsed - last);
        }

        Benchmark = TimeSpan.FromTicks(Results.Select(x => x.Ticks).Sum() / Results.Count);
        Log.Write("Benchmark: {Benchmark}", Benchmark);
        Succeeded = true;
        Elapsed = Log.Finish().Elapsed;
      }
      catch (TargetInvocationException ex)
      {
        Succeeded = false;
        Error = ex.InnerException.Message;

        Elapsed = Log.Failed(ex).Elapsed;
      }
    }
  }
}
