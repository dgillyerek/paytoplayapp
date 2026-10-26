using System.Text.RegularExpressions;

namespace Survival.Domain.Tests;

/// <summary>
/// The Android CI build signs with a key kept only in repo secrets and builds with the project's exact editor version;
/// these guard the parts a stray commit or an odd version string would silently break.
/// </summary>
public sealed class AndroidCiTests
{
    private static readonly string Root = FindRepoRoot();

    [Fact]
    public void Editor_version_has_a_matching_android_ci_image_tag_format()
    {
        var version = File.ReadAllLines(Path.Combine(Root, "ProjectSettings", "ProjectVersion.txt"))
            .First(l => l.StartsWith("m_EditorVersion:", StringComparison.Ordinal)).Split(':')[1].Trim();
        Assert.Matches(@"^\d+\.\d+\.\d+f\d+$", version);
    }

    [Fact]
    public void Signing_keys_are_gitignored_and_none_are_in_the_repo()
    {
        var ignore = File.ReadAllText(Path.Combine(Root, ".gitignore"));
        Assert.Contains("*.keystore", ignore, StringComparison.Ordinal);
        Assert.Contains("*.jks", ignore, StringComparison.Ordinal);
        var skip = new[] { "Library", "Temp", "Builds", "build", ".git", "bin", "obj" };
        var found = new List<string>();
        var stack = new Stack<string>(new[] { Root });
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            foreach (var sub in Directory.GetDirectories(dir))
            {
                if (!skip.Contains(Path.GetFileName(sub)))
                {
                    stack.Push(sub);
                }
            }

            found.AddRange(Directory.GetFiles(dir).Where(f => f.EndsWith(".keystore", StringComparison.OrdinalIgnoreCase) ||
                                                             f.EndsWith(".jks", StringComparison.OrdinalIgnoreCase)));
        }

        Assert.Empty(found);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectSettings", "ProjectVersion.txt")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }
}
