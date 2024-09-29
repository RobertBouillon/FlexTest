using System;
using System.Collections.Generic;
using System.Linq;

namespace Spin.FlexTest;
public class BenchmarkResults
{
  public List<TimeSpan> Results { get; } = new();
  public List<TimeSpan> WarmupResults { get; } = new();
  public Dictionary<string, object> Metrics { get; } = new();

  public TimeSpan? TestDuration { get; internal set; }
  public TimeSpan? Benchmark { get; internal set; }
  public TimeSpan? Delta => Benchmark.HasValue && Average.HasValue ? Average.Value - Benchmark.Value : null;
  public TimeSpan? Variance => Results.Count > 1 ? Results.Max() - Results.Min() : null;
  public double? Deviation => Results.Any() ? Results.Select(x => (double)x.Ticks).StdDev() : null;
  public TimeSpan? Average => Results.Any() ? TimeSpan.FromTicks(Results.Sum(x => x.Ticks) / Results.Count) : null;

  public bool Succeeded { get; internal set; }
  public string Error { get; internal set; }

  public void Add(TimeSpan duration, bool isWarmup)
  {
    if (isWarmup)
      WarmupResults.Add(duration);
    else
      Results.Add(duration);
  }
}
