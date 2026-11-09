using Survival.Domain.Kids;

namespace Survival.Domain.Tests;

internal sealed class MemoryKidStorage : IKidSafetyStorage
{
    public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);

    public string? Get(string key) => Values.TryGetValue(key, out var v) ? v : null;

    public void Set(string key, string value) => Values[key] = value;

    public void Delete(string key) => Values.Remove(key);

    public void Save()
    {
    }
}

internal static class KidsFiles
{
    public static string RepoRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
        {
            if (File.Exists(Path.Combine(d.FullName, "Packages", "manifest.json")) && Directory.Exists(Path.Combine(d.FullName, "Assets")))
            {
                return d.FullName;
            }
        }

        throw new DirectoryNotFoundException("repo root");
    }

    public static string ConfigPath => Path.Combine(RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", "kids", ChildSafetyConfig.FileName);

    public static ChildSafetyConfig Shipped() => ChildSafetyConfig.Parse(File.ReadAllText(ConfigPath));

    public static ChildSafetyPolicy Policy(ChildSafetyConfig config, string? band) =>
        new(config, band == null ? new AgeGateState(AgeGateStatus.Fresh, null) : new AgeGateState(AgeGateStatus.Answered, band));
}
