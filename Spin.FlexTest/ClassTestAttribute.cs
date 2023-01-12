using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Spin.FlexTest;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class ClassTestAttribute : TestAttribute
{
  public string Name { get; set; }

  public ClassTestAttribute(){}
  public ClassTestAttribute(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));

  private static Regex _parser = new Regex(@"Test([\w\d_]+)", RegexOptions.Compiled);
  public override string GetName(MethodInfo method)
  {
    var ns = method.DeclaringType.Namespace;
    var classname = method.DeclaringType.Name;
    var name = Name ?? _parser.Match(method.Name).Groups[1].Value;

    return String.IsNullOrWhiteSpace(name)?
      $"{ns}.{classname}":
      $"{ns}.{classname}:{name}";
  }
}
