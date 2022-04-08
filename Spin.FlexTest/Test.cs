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

namespace Spin.FlexTest
{
  public class Test
  {
    public string Name { get; set; }
    public MethodInfo Target { get; }
    public Stopwatch Stopwatch { get; }
    public TimeSpan Elapsed { get; private set; }
    public Boolean Succeeded { get; private set; } = true;
    public string Error { get; private set; }

    public Dictionary<String, TestMetric> Metrics { get; set; } = new Dictionary<string, TestMetric>();

    public Test(MethodInfo target)
    {
      #region Validation
      if (target == null)
        throw new ArgumentNullException(nameof(target));
      #endregion

      Name = target.GetCustomAttribute<TestAttribute>().GetName(target);
      Target = target;
      Stopwatch = new Stopwatch();

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
        Execute(() => Target.Invoke(Activator.CreateInstance(Target.ReflectedType), Array.Empty<Object>()));
    }

    private void Execute(Action action)
    {
      Stopwatch sw = new Stopwatch();
      sw.Start();
      try
      {
        action();
        Succeeded = true;
      }
      catch (TargetInvocationException ex)
      {
        Succeeded = false;
        Error = ex.InnerException.Message;
        Log.DefaultScope.Write("{Name} failed", Name);
      }
      sw.Stop();
      Elapsed = sw.Elapsed;
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
      FluentTry.Try(action).Catch(x => failed = validator(x));
      if (!failed)
        Fail($"{description} did not fail as expected");
    }

    public override string ToString() => Name;
  }
}
