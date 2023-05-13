﻿using Spin.Pillars.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Spin.FlexTest;

public abstract class TestFixture : IDisposable
{
  public Test ExecutingTest { get; protected internal set; }

  public static IEnumerable<Type> Gather(Assembly assembly) => assembly
    .GetTypes()
    .Where(IsTestFixture)
    .Where(x => !x.IsAbstract);

  public static bool IsTestFixture(Type type)
  {
    do
    {
      if (type.BaseType == typeof(TestFixture))
        return true;
      type = type.BaseType;
    } while (type != null);
    return false;
  }

  public virtual string Name => GetType().Name;
  public LogScope Log { get; set; }
  public virtual bool CanReuse => false;

  protected void Fail(string reason = null) => ExecutingTest.Fail(reason);

  protected void Assert(bool condition, string reason = null)
  {
    if (!condition)
      Fail(reason);
  }

  protected void ShouldFail(Action action, Func<Exception, bool> validator = null, string description = null)
  {
    if (validator == null)
      validator = x => true;
    bool failed = false;
    FluentTry.Try(action).Catch(x => failed = validator(x));
    if (!failed)
      Fail($"{description} did not fail as expected");
  }

  protected Test CreateTest(string name, Action action) => new Test(name, action, Log);

  public virtual void InitializeMethod() { }
  public virtual void Dispose() { }
  public virtual IEnumerable<Test> GatherTests()
  {
    var tests = GetType()
      .GetMethods(BindingFlags.Public | BindingFlags.Instance)
      .Where(x => x.ReturnType == typeof(IEnumerable<Test>) && x.GetParameters().Length == 0)
      .SelectMany(x => (IEnumerable<Test>)x.Invoke(this, Array.Empty<Object>()))
      .Concat(Test.Gather(Log, GetType()))
      .ToList();

    foreach (var test in tests)
      test.Fixture = this;

    return tests;
  }
}
