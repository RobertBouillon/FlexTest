using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spin.FlexTest
{
  [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
  public class PerformanceTestAttribute : TestAttribute
  {
    public int Iterations { get; set; }
    public int Warmup { get; set; }

    // This is a positional argument
    public PerformanceTestAttribute() { }

  }
}
