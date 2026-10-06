using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BenchmarkContextAttribute : Attribute
{
  public static Attempt<BenchmarkContextAttribute> TryFind(Type type)
  {
    var attribute = type.GetCustomAttributes().OfType<BenchmarkContextAttribute>().FirstOrDefault();
    return attribute ?? new Attempt<BenchmarkContextAttribute>(false);
  }
  
  public string Category { get; set; }

  public BenchmarkContextAttribute() { }
  public BenchmarkContextAttribute(string category) => Category = category;
  
}

