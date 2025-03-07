﻿using Spin.Pillars.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Spin.FlexTest;

public abstract class TestFixture : IDisposable
{
  protected int DefaultRandomSeed { get; set; } = 123456;
  private int _randomSeed;
  protected Random Random { get; set; }

  public Test ExecutingTest { get; protected internal set; }
  public Benchmark ExecutingBenchmark { get; protected internal set; }

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

  protected int RandomSeed
  {
    get => _randomSeed;
    set => Random = new(_randomSeed = value);
  }

  //protected void Fail(string reason = null) => ExecutingTest.Fail(reason);
  protected void Fail(string reason = null) => throw new Exception(reason); //Stop executing the test. Find a better way do to this that isn't so noisy (e.g. Assert() should stop execution without needing if(!Assert()) return;
  public IEnumerable<T> GenerateRandom<T>(int count = 100) => Enumerable.Range(0, count).Select(_=>(T)Convert.ChangeType(Random.Next(), typeof(T)));

  protected virtual void Assert(bool condition, string reason = null)
  {
    if (!condition)
      Fail(reason);
  }

  protected void ShouldFail(Action action, Func<Exception, bool> validator = null, string description = null)
  {
    validator ??= x => true;
    bool failed = false;
    FluentTry.Try(action).Catch(x => failed = validator(x));
    if (!failed)
      Fail($"{description} did not fail as expected");
  }

  protected Test CreateTest(MethodInfo generator, Action action, string augment)
  {
    var attribute = generator.GetCustomAttribute<TestAttribute>();
    if (attribute is null)
      throw new Exception($"{generator} is not decordated with a Test attribute");
    return new Test(attribute, generator, Log, augment, action);
  }

  public virtual void OnTestStarting() => Random = new Random(_randomSeed = DefaultRandomSeed); 
  public virtual void OnTestFinished() { }
  public virtual void Dispose() { }
  public virtual IEnumerable<Test> GatherTests(LogScope log)
  {
    Log = log;
    var tests = GetType()
      .GetMethods(BindingFlags.Public | BindingFlags.Instance)
      .Where(x => x.HasCustomAttribute<TestAttribute>() && x.ReturnType == typeof(IEnumerable<Test>) && x.GetParameters().Length == 0)
      .SelectMany(x => (IEnumerable<Test>)x.Invoke(this, Array.Empty<Object>()))
      .Concat(Test.Gather(Log, GetType()))
      .ToList();

    foreach (var test in tests)
      test.Fixture = this;

    return tests;
  }

  public virtual IEnumerable<Benchmark> GatherBenchmarks(LogScope log)
  {
    Log = log;
    var benchmarks = GetType()
      .GetMethods(BindingFlags.Public | BindingFlags.Instance)
      .Where(x => x.HasCustomAttribute<BenchmarkAttribute>() && x.ReturnType == typeof(IEnumerable<Benchmark>) && x.GetParameters().Length == 0)
      .SelectMany(x => (IEnumerable<Benchmark>)x.Invoke(this, Array.Empty<Object>()))
      .Concat(Benchmark.Gather(Log, GetType()))
      .ToList();

    foreach (var test in benchmarks)
      test.Fixture = this;

    return benchmarks;
  }
}
