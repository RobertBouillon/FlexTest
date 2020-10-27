using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spin.FlexTest
{
  [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
  public sealed class TestDependenciesAttribute : Attribute
  {
    public string[] DependentTests { get; }
    
    public TestDependenciesAttribute(params string[] dependentTests) =>
      DependentTests = dependentTests ?? throw new ArgumentNullException(nameof(dependentTests));
  }
}
