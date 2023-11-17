using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

using Spin.Pillars.Hierarchy;

namespace Spin.FlexTest;
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public partial class BenchmarkAttribute : Attribute
{
  [GeneratedRegex(@"(Benchmark)?(?<name>.+)", RegexOptions.Compiled)]
  private static partial Regex GenerateNameRegex();
  private static Regex _nameParser = GenerateNameRegex();

  private string _name;
  private bool _isInitialized = false;
  public string Name
  {
    get => _isInitialized ? _name : throw new Exception("Not intialized");
  }
  public string Category { get; set; }
  public int WarmupIterations { get; set; } = 1;
  public int TestIterations { get; set; } = 3;

  public BenchmarkAttribute() { }

  //C# lacks a way to initialize an attribute based on its target, so we do that manually here.
  public void Intialize(MethodInfo method)
  {
    if (_isInitialized)
      throw new InvalidOperationException("Already initialized");

    _name ??= ParseName(method);

    _isInitialized = true;
  }

  private string ParseName(MethodInfo method)
  {
    var name = _nameParser.Match(method.Name).Groups["name"].Value;

    var parent = method.ReflectedType;
    if (TestFixture.IsTestFixture(method.ReflectedType) && method.ReflectedType.ReflectedType is not null)
      parent = method.ReflectedType.ReflectedType;

    var parentName = parent.GetCustomAttribute<DescriptionAttribute>()?.Description ?? parent.Name;
    return name == "Test" ? parentName : $"{parentName}:{name}";
  }
}

