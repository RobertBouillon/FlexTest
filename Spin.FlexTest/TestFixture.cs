using Spin.Pillars.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spin.FlexTest;

public abstract class TestFixture : IDisposable
{
  public static bool IsTestFixture(Type type)
  {
    do
    {
      if (type.BaseType == typeof(TestFixture))
        return true;
      type = type.BaseType;
    } while (type != null);
    return false;
  }

  public virtual string Name => GetType().Name;
  public LogScope Log { get; set; }
  public virtual bool CanReuse => false;

  protected static void Fail(string reason = null) => throw new Exception(reason);

  protected static void Assert(bool condition, string reason = null)
  {
    if (!condition)
      Fail(reason);
  }

  protected void ShouldFail(Action action, Func<Exception, bool> validator = null, string description = null)
  {
    if (validator == null)
      validator = x => true;
    bool failed = false;
    FluentTry.Try(action).Catch(x => failed = validator(x));
    if (!failed)
      Fail($"{description} did not fail as expected");
  }

  public virtual void Initialize() { }
  public virtual void Cleanup() { }
  public virtual void Dispose() { }
}
