using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Spin.FlexTest;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class TestAttribute : Attribute
{
  private static Regex _nameParser = new Regex(@"(Test)?(?<name>.+)", RegexOptions.Compiled);
  private string Name { get; }  //Use GetName instead

  public TestAttribute() { }
  public TestAttribute(string name)
  {
    #region Validation
    if (String.IsNullOrWhiteSpace(name))
      throw new ArgumentNullException(nameof(name));
    #endregion
    Name = name;
  }

  public virtual string GetName(MethodInfo method)
  {
    var name = _nameParser.Match(method.Name).Groups["name"].Value;

    var parent = method.DeclaringType;
    if (TestFixture.IsTestFixture(method.DeclaringType) && method.DeclaringType.DeclaringType is not null)
      parent = method.DeclaringType.DeclaringType;
    
    var parentName = parent.GetCustomAttribute<DescriptionAttribute>()?.Description ?? parent.Name;
    return name == "Test" ?
      Name ?? parentName :
    Name ?? parentName + ":" + name;
  }
}
