using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Spin.Pillars.Logging;
using static System.FluentTry;

namespace Spin.FlexTest;

public class Test
{
  public static IEnumerable<Test> Gather(LogScope log, params Type[] types) => Gather(log, types.Cast<Type>());
  public static IEnumerable<Test> Gather(LogScope log, IEnumerable<Type> types) => types
    .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
    .Select(x => (TestAttribute: x.GetCustomAttribute<TestAttribute>(), Method: x))
    .Where(x => x.TestAttribute is not null && x.Method.ReturnType == typeof(void))
    .Select(x => new Test(x.TestAttribute, x.Method, log));

  public static IEnumerable<Test> Gather(LogScope log, params Assembly[] assemblies) => Gather(log, (IEnumerable<Assembly>)assemblies);
  public static IEnumerable<Test> Gather(LogScope log, IEnumerable<Assembly> assemblies) => Gather(log, assemblies.SelectMany(x => x.GetTypes()));

  public TestFixture Fixture { get; set; }
  public string Name { get; set; }
  public Action Action { get; }
  public TimeSpan Elapsed { get; protected set; }
  public bool Succeeded { get; protected set; } = true;
  public string Error { get; protected set; }
  public LogScope Log { get; }

  public Dictionary<String, TestMetric> Metrics { get; set; } = new Dictionary<string, TestMetric>();

  internal Test(TestAttribute attribute, MethodInfo target, LogScope parentLog)
  {
    Name = attribute.GetName(target);
    Action = () => target.Invoke(Fixture, Array.Empty<Object>());
    Log = parentLog.AddScope(Name);

    if (target.GetParameters().Count() > 0)
      throw new Exception($"{target.DeclaringType.FullName}.{target.Name} cannot have parameters");
  }

  internal Test(string name, Action action, LogScope parentLog)
  {
    Name = name;
    Action = action;
    Log = parentLog.AddScope(Name);
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

  public void Execute() => Execute(Action);

  protected virtual void Execute(Action action)
  {
    if (Fixture is not null)
      Fixture.ExecutingTest = this;

    Log.Start();
    try
    {
      Fixture.InitializeMethod();
      action();
      Elapsed = Log.Finish().Elapsed;
    }
    catch (TargetInvocationException ex)
    {
      Elapsed = Log.Failed(ex.InnerException).Elapsed;
      Succeeded = false;
      Error = ex.InnerException.Message;
    }

    if (Fixture is not null)
      Fixture.ExecutingTest = null;
  }

  public void Fail(string reason = null)
  {
    Succeeded = false;
    Error = reason;
  }

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
