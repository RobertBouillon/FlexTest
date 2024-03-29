﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Spin.Pillars.Logging;
using static System.FluentTry;
using System.Diagnostics;

namespace Spin.FlexTest;

public class Test
{
  public static IEnumerable<Test> Gather(LogScope log, params Type[] types) => Gather(log, types.Cast<Type>());
  public static IEnumerable<Test> Gather(LogScope log, IEnumerable<Type> types) => types
    .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
    .Select(x => (TestAttribute: x.GetCustomAttribute<TestAttribute>(), Method: x, Name: x.Name))
    .Where(x => x.TestAttribute is not null && x.Method.ReturnType == typeof(void) && (x.TestAttribute.Inherit || x.Method.DeclaringType == x.Method.ReflectedType))
    .Select(x => new Test(x.TestAttribute, x.Method, log));

  public static IEnumerable<Test> Gather(LogScope log, params Assembly[] assemblies) => Gather(log, (IEnumerable<Assembly>)assemblies);
  public static IEnumerable<Test> Gather(LogScope log, IEnumerable<Assembly> assemblies) => Gather(log, assemblies.SelectMany(x => x.GetTypes()));

  public TestFixture Fixture { get; set; }
  public string Name { get; set; }
  public string TestExplorerName { get; set; }
  public string SourceFile { get; set; }
  public int SourceLineNumber { get; set; }
  public Action Action { get; }
  public TimeSpan Duration { get; protected set; }
  public bool Succeeded { get; protected set; } = true;
  public string Error { get; protected set; }
  public LogScope Log { get; }
  public string Category { get; set; }
  public Stopwatch Timer { get; } = new();

  public Dictionary<String, TestMetric> Metrics { get; set; } = new Dictionary<string, TestMetric>();

  internal Test(TestAttribute attribute, MethodInfo target, LogScope parentLog)
  {
    Name = attribute.GetName(target);
    Category = attribute.Category;
    TestExplorerName = attribute.GetFullName(target);
    Action = () => target.Invoke(Fixture, Array.Empty<Object>());
    Log = parentLog.AddScope(Name);
    SourceFile = attribute.File;
    SourceLineNumber = attribute.Line;

    if (target.GetParameters().Count() > 0)
      throw new Exception($"{target.DeclaringType.FullName}.{target.Name} cannot have parameters");
  }

  internal Test(TestAttribute attribute, MethodInfo generator, LogScope parentLog, string augment, Action action)
  {
    Name = attribute.GetName(generator) + augment;
    Category = attribute.Category;
    TestExplorerName = attribute.GetFullName(generator) + augment;
    Action = action;
    Log = parentLog.AddScope(Name);
    SourceFile = attribute.File;
    SourceLineNumber = attribute.Line;
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

    var sw = new System.Diagnostics.Stopwatch();
    sw.Start();  //HACK: Logging duration is bugged
    Log.Start();
    try
    {
      Fixture?.OnTestStarting();
      Timer.Start();
      action();
      Timer.Stop();
      Fixture?.OnTestFinished();
      Duration = Timer.Elapsed;
      Timer.Stop();
      Log.Finish();
    }
    catch (TargetInvocationException ex)
    {
      Duration = Log.Failed(ex.InnerException).Elapsed;
      Succeeded = false;
      Error = ex.InnerException.Message;
    }
    catch (Exception ex)
    {
      Duration = Log.Failed(ex).Elapsed;
      Succeeded = false;
      Error = ex.Message;
    }

    if (Fixture is not null)
      Fixture.ExecutingTest = null;
    Duration = sw.Elapsed;
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
    validator ??= x => true;
    bool failed = false;
    Try(action).Catch(x => failed = validator(x));
    if (!failed)
      Fail($"{description} did not fail as expected");
  }

  public override string ToString() => Name;
}
