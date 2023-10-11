using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Spin.FlexTest;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class TestAttribute : Attribute
{
  private static Regex _nameParser = new Regex(@"(Test)?(?<name>.+)", RegexOptions.Compiled);
  private string Name { get; }  //Use GetName instead
  public string Category { get; set; } = "Unit Tests";
  public string File { get; }
  public int Line { get; }

  public TestAttribute(
    [CallerFilePath] string file = "",
    [CallerLineNumber] int line = 1)
  {
    File = file;
    Line = line;
  }
  public TestAttribute(
    string name,
    [CallerFilePath] string file = "",
    [CallerLineNumber] int line = 1)
  {
    #region Validation
    if (String.IsNullOrWhiteSpace(name))
      throw new ArgumentNullException(nameof(name));
    #endregion
    Name = name;
    File = file;
    Line = line;
  }

  public virtual string GetName(MethodInfo method)
  {
    var name = _nameParser.Match(method.Name).Groups["name"].Value;

    var parent = method.ReflectedType;
    if (TestFixture.IsTestFixture(method.ReflectedType) && method.ReflectedType.ReflectedType is not null)
      parent = method.ReflectedType.ReflectedType;

    var parentName = parent.GetCustomAttribute<DescriptionAttribute>()?.Description ?? parent.Name;
    return name == "Test" ?
      Name ?? parentName :
    Name ?? parentName + ":" + name;
  }

  public virtual string GetFullName(MethodInfo method)
  {
    var name = _nameParser.Match(method.Name).Groups["name"].Value;

    var parent = method.ReflectedType;
    if (TestFixture.IsTestFixture(method.ReflectedType) && method.ReflectedType.ReflectedType is not null)
      parent = method.ReflectedType.ReflectedType;

    var parentNamespace = parent.Namespace;
    var parentName = parent.GetCustomAttribute<DescriptionAttribute>()?.Description ?? parent.Name;
    return parentNamespace + "." + (Name ?? parentName + "." + name);

  }
}
