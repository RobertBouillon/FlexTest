using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using Spin.Pillars.Logging;
using Spin.Pillars.Logging.Data;
using static System.FluentTry;

namespace Spin.FlexTest
{
  public class Test
  {
    public static Test Create(MethodInfo target, LogScope parentLog)
    {
      #region Validation
      if (target == null)
        throw new ArgumentNullException(nameof(target));
      if (parentLog is null)
        throw new ArgumentNullException(nameof(parentLog));
      #endregion

      var attribute = target.GetCustomAttribute<TestAttribute>();

      if (attribute.Type == TestType.Performance)
        return new PerformanceTest(attribute, target, parentLog);
      else
        return new Test(attribute, target, parentLog);
    }

    public string Name { get; set; }
    public MethodInfo Target { get; }
    public Stopwatch Stopwatch { get; }
    public TimeSpan Elapsed { get; protected set; }
    public bool Succeeded { get; protected set; } = true;
    public string Error { get; protected set; }
    public TestType Type { get; }
    public LogScope Log { get; }

    public Dictionary<String, TestMetric> Metrics { get; set; } = new Dictionary<string, TestMetric>();

    protected Test(TestAttribute attribute, MethodInfo target, LogScope parentLog)
    {
      Name = attribute.GetName(target);
      Type = attribute.Type;
      Target = target;
      Stopwatch = new Stopwatch();
      Log = parentLog.AddScope(Name);

      if (target.GetParameters().Count() > 0)
        throw new Exception($"{target.Name} cannot have parameters");
    }

    public void SetMetric(string name, object value, string displayValue = null)
    {
      if (!Metrics.TryGetValue(name, out var metric))
        Metrics.Add(name, new TestMetric(name, value, displayValue));
      else
      {
        metric.Value = value;
        metric.DisplayValue = displayValue;
      }
    }

    public void Execute()
    {
      if (!typeof(TestFixture).IsAssignableFrom(Target.ReflectedType))
        if (Target.IsStatic)
          Execute(() => Target.Invoke(null, Array.Empty<Object>()));
        else
          throw new Exception($"{Target.ReflectedType} is not a Test Fixture");
      else
      {
        var fixture = Activator.CreateInstance(Target.ReflectedType) as TestFixture;
        fixture.Log = Log.AddScope(fixture.Name);

        Execute(() => Target.Invoke(fixture, Array.Empty<Object>()));
      }
    }

    protected virtual void Execute(Action action)
    {
      Log.Start();
      try
      {
        action();
        Elapsed = Log.Finish().Elapsed;
      }
      catch (TargetInvocationException ex)
      {
        Elapsed = Log.Failed(ex.InnerException).Elapsed;
        Succeeded = false;
        Error = ex.InnerException.Message;
      }
    }

    public void Fail(string reason = null) => throw new Exception(reason);

    public void Assert(bool condition, string reason = null)
    {
      if (!condition)
        Fail(reason);
    }

    public void ShouldFail(Action action, Func<Exception, bool> validator = null, string description = null)
    {
      if (validator == null)
        validator = x => true;
      bool failed = false;
      Try(action).Catch(x => failed = validator(x));
      if (!failed)
        Fail($"{description} did not fail as expected");
    }

    public override string ToString() => Name;
  }
}
