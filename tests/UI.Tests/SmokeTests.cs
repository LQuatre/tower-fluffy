using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TowerFluffy.UI.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void DesktopUi_ProjectReferences_ApplicationLayer()
    {
        var repoRoot = FindRepoRootDirectory();
        var uiProjectPath = Path.Combine(repoRoot.FullName, "src", "UI.Desktop", "TowerFluffy.UI.Desktop.csproj");

        Assert.True(File.Exists(uiProjectPath), $"UI project file not found: {uiProjectPath}");

        var document = XDocument.Load(uiProjectPath);
        var projectReferences = document.Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(static v => !string.IsNullOrWhiteSpace(v))
            .Select(static v => v!)
            .ToArray();

        Assert.Contains(
            projectReferences,
            reference => reference.EndsWith("Application\\TowerFluffy.Application.csproj", StringComparison.OrdinalIgnoreCase));
    }

    private static DirectoryInfo FindRepoRootDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "TowerFluffy.slnx")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root directory (missing TowerFluffy.slnx)");
    }
}
