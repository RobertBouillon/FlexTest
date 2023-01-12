using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Spin.FlexTest;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class UtilityAttribute : Attribute
{
  readonly string _name;
  public string Name => _name;

  public UtilityAttribute() { }
  public UtilityAttribute(string name) => _name = name ?? throw new ArgumentNullException(nameof(name));

  public virtual string GetName(MethodInfo method) => Name ?? method.Name;
}
