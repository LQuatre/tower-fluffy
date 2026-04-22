using System.Linq;
using TowerFluffy.Application;
using Xunit;

namespace TowerFluffy.Application.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Application_DoesNotReference_UI_Or_Infrastructure()
    {
        var referencedAssemblyNames = typeof(AssemblyReference).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToArray();

        Assert.DoesNotContain(referencedAssemblyNames, name => name is not null && name.StartsWith("TowerFluffy.UI"));
        Assert.DoesNotContain("TowerFluffy.Infrastructure", referencedAssemblyNames);
    }
}
