using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spin.FlexTest;

public class TestMetric
{
  public String Name { get; set; }
  public Object Value { get; set; }
  public String DisplayValue { get; set; }

  public TestMetric(string name, object value, string displayValue = null)
  {
    #region Validation
    if (String.IsNullOrWhiteSpace(name))
      throw new ArgumentNullException(nameof(name));
    if (value == null)
      throw new ArgumentNullException(nameof(value));
    #endregion
    Name = name;
    Value = value;
    DisplayValue = displayValue ?? value.ToString();
  }

  public override string ToString() => $"{Name}: {DisplayValue}";
}
