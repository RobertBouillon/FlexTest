using System;
using System.Collections.Generic;
using System.Text;

namespace Spin.FlexTest
{
  public class Milestone
  {
    public string Name { get; set; }
    public TimeSpan Time { get; set; }

    public Milestone(String name, TimeSpan time)
    {
      #region Validation
      if (String.IsNullOrWhiteSpace(name))
        throw new ArgumentNullException(nameof(name));
      #endregion

      Name = name;
      Time = time;
    }
  }
}
