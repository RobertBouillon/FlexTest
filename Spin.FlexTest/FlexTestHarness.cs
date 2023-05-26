using System.Collections.Generic;

namespace Spin.FlexTest;
public abstract class FlexTestHarness
{
  public abstract IEnumerable<Test> DiscoverTests();
}
