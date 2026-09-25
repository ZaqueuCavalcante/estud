using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Estud.Tests.Domain;

public class EnumExplicitValuesUnitTests
{
    [Test]
    public void EnumExplicitValues_Should_have_explicit_integer_values_for_all_enum_members()
    {
        var missing = new List<string>();

        // Reflection não distingue `Pending` de `Pending = 0` (o valor já vem compilado), então a checagem é feita no fonte.
        var backDir = Path.Combine(FindRepoRoot(), "Back");
        var files = Directory.EnumerateFiles(backDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

        foreach (var file in files)
        {
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();

            foreach (var enumDecl in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
            {
                foreach (var member in enumDecl.Members.Where(m => m.EqualsValue is null))
                {
                    var line = member.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    missing.Add($"{enumDecl.Identifier.Text}.{member.Identifier.Text} ({Path.GetRelativePath(backDir, file)}:{line})");
                }
            }
        }

        missing.Should().BeEmpty("enum members without explicit value:\n" + string.Join("\n", missing));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Estud.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Estud.slnx not found");
    }
}
