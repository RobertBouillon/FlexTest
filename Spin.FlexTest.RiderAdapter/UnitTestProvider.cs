using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.ReSharper.UnitTestFramework.Execution;
using JetBrains.ReSharper.UnitTestFramework.Execution.Hosting;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace Spin.FlexTest.RiderAdapter;

public class UnitTestProvider : IUnitTestProvider
{
  public bool IsElementOfKind(IDeclaredElement declaredElement, UnitTestElementKind elementKind)
  {
    
    throw new NotImplementedException();
  }

  public bool IsElementOfKind(IUnitTestElement element, UnitTestElementKind elementKind)
  {
    throw new NotImplementedException();
  }

  public bool IsSupported(IHostProvider hostProvider, IProject project, TargetFrameworkId targetFrameworkId)
  {
    throw new NotImplementedException();
  }

  public bool IsSupported(IProject project, TargetFrameworkId targetFrameworkId) => true;

  public bool SupportsResultEventsForParentOf(IUnitTestElement element)
  {
    throw new NotImplementedException();
  }

  public IUnitTestRunStrategy GetRunStrategy(IUnitTestElement element, IHostProvider hostProvider)
  {
    throw new NotImplementedException();
  }

  public string ID => "SPIN.FlexTest";
  public string Name => "Flex Test";
}