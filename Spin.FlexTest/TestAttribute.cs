﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Spin.FlexTest
{
  [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
  public class TestAttribute : Attribute
  {
    private static Regex _nameParser = new Regex(@"(Test)?(?<name>.+)", RegexOptions.Compiled);
    private string Name { get; }

    public TestAttribute() { }
    public TestAttribute(string name)
    {
      #region Validation
      if (String.IsNullOrWhiteSpace(name))
        throw new ArgumentNullException(nameof(name));
      #endregion
      Name = name;
    }

    public virtual string GetName(MethodInfo method) => Name ?? method.DeclaringType.Name + ":" + _nameParser.Match(method.Name).Groups["name"].Value;
  }
}
