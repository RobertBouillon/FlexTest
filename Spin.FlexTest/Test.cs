using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using Spin.Pillars;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using Spin.Pillars.Logging;

namespace Spin.FlexTest
{
  public class Test
  {
    public static T SerializeJson<T>(Action<JsonWriter> serialize, Func<JToken, T> deserialize, int buffer = 4096)
    {
      using (var ms = new MemoryStream(buffer))
      {
        using (var sw = new StreamWriter(ms, System.Text.Encoding.UTF8, buffer, true))
        using (var json = new JsonTextWriter(sw))
          serialize(json);

        ms.Position = 0;

        using (var sr = new StreamReader(ms))
          return deserialize(JsonConvert.DeserializeObject(sr.ReadToEnd()) as JToken);
      }
    }

    public string Name { get; set; }
    public MethodInfo Target { get; }
    //public List<Test> Dependencies { get; } = new List<Test>();
    public Stopwatch Stopwatch { get; }
    public List<Milestone> Milestones { get; } = new List<Milestone>();
    public Pillars.Module Module { get; }
    public TimeSpan Elapsed { get; private set; }
    public Boolean Succeeded { get; private set; } = true;
    public Dictionary<String, TestMetric> Metrics { get; set; } = new Dictionary<string, TestMetric>();
    public string FailureReason { get; private set; }
    public List<Type> InjectionDependencies { get; }
    public Type ReturnsObject { get; }
    public List<Test> DependentTests { get; internal set; }

    public Test(Pillars.Module module, MethodInfo target)
    {
      #region Validation
      if (target == null)
        throw new ArgumentNullException(nameof(target));
      if (module == null)
        throw new ArgumentNullException(nameof(module));
      #endregion

      Name = target.GetCustomAttribute<TestAttribute>().GetName(target);
      Module = module;
      Target = target;
      Stopwatch = new Stopwatch();
      ReturnsObject = target.ReturnType;
    }

    internal void PopulateDependencies(Tests tests)
    {
      if (!Target.HasCustomAttribute<TestDependenciesAttribute>())
      {
        DependentTests = new List<Test>();
        return;
      }

      List<string> testnames = new List<string>(Target.GetCustomAttribute<TestDependenciesAttribute>().DependentTests);
      DependentTests = new List<Test>(testnames.SelectMany(x => tests.Where(y => new Regex(x).IsMatch(y.Name))));
    }

    public Milestone SetMilestone(String name)
    {
      var ms = new Milestone(name, Stopwatch.Elapsed);
      Milestones.Add(ms);
      return ms;
    }

    public void SetMetric(string name, object value, string displayValue = null)
    {
      if (!Metrics.TryGetValue(name, out var metric))
        Metrics.Add(name, metric = new TestMetric(name, value, displayValue));
      else
      {
        metric.Value = value;
        metric.DisplayValue = displayValue;
      }
    }


    public void Run(Dictionary<Type, Object> objectCache)
    {
      var args = Target.GetParameters().Select(x => GetDependency(x, objectCache)).ToArray();
      Stopwatch.Start();
      try
      {
        var result = Target.Invoke(null, args);
        Stopwatch.Stop();
        if (Succeeded &= String.IsNullOrEmpty(FailureReason))
          objectCache[ReturnsObject] = result;
      }
      catch (TargetInvocationException ex)
      {
        Stopwatch.Stop();
        Succeeded = false;
        Module.Log.Write(ex.InnerException);
        FailureReason = ex.InnerException.Message;
      }
      Elapsed = Stopwatch.Elapsed;

      Module.Log.Write("Test completed in {1}", Name, Stopwatch.Elapsed);
      foreach (var meter in Metrics)
        Module.Log.Write("{0,-12}: {1}", meter.Key, meter.Value.DisplayValue);
    }

    public void Fail(string reason = null)
    {
      Module.Log.Write(LogSeverity.Error, reason);
      Succeeded = false;
      FailureReason = reason;
      throw new Exception(reason ?? $"'{Name}' did not operate as expected");
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
      FluentTry.Try(action).Catch(x => failed = validator(x));
      if (!failed)
        Fail($"{description} did not fail as expected");
    }

    public bool HasDependencies(ISet<Type> objectCache, HashSet<Test> testsRun) =>
      Target.GetParameters().All(x => objectCache.Contains(x.ParameterType)) && !DependentTests.Any(x => !testsRun.Contains(x));

    public IEnumerable<Type> GetMissingDependencies(ISet<Type> objectCache) => Target.GetParameters().Select(x => x.ParameterType).Where(x => !objectCache.Contains(x));
    //public IEnumerable<Type> GetMissingDependencies(ISet<Type> objectCache)
    //{
    //  var missing = Target.GetParameters().Select(x => x.ParameterType).Where(x => !objectCache.Contains(x)).ToList();
    //  if(missing.Count > 0)
    //    Console.WriteLine();
    //  return missing;
    //}

    private object GetDependency(ParameterInfo parameter, Dictionary<Type, Object> objectCache)
    {
      if (parameter.ParameterType == typeof(Test))
        return this;
      else if (parameter.ParameterType == typeof(Pillars.Module))
        return Module;
      else if (objectCache.TryGetValue(parameter.ParameterType, out var dependency))
        return dependency;
      else
        throw new NotSupportedException($"Unable to find dependecy of type '{parameter.ParameterType}' on '{parameter.Member.DeclaringType}.{parameter.Member.Name}'");
    }

    public override string ToString() => Name;
  }
}
