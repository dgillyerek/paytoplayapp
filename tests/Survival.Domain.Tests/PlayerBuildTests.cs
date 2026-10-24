using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>
/// Phone (player) builds have no AssetDatabase: every roster character must be in the Editor bake manifest, its files must
/// exist, runtime-read textures must be importer-readable, and the runtime assemblies must not touch UnityEditor.
/// </summary>
public sealed class PlayerBuildTests
{
    [Fact]
    public void Manifest_has_one_entry_per_roster_fighter_in_roster_order()
    {
        var names = LockedBattleRoster.All.Select(f => f.Name).ToArray();
        Assert.Equal(12, names.Length);
        Assert.Equal(names, PlayerBakeManifest.All.Select(f => f.Name).ToArray());
        foreach (var name in names)
        {
            var fighter = PlayerBakeManifest.For(name);
            Assert.NotNull(fighter);
            Assert.NotEmpty(fighter!.Assets);
            Assert.Contains(fighter.Assets, a => a.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(fighter.Assets, a => IsTexture(a));
        }
    }

    [Fact]
    public void Every_manifest_asset_and_raw_file_exists_under_Assets()
    {
        var root = FindRepoRoot();
        var missing = new List<string>();
        foreach (var fighter in PlayerBakeManifest.All)
        {
            foreach (var rel in fighter.Assets.Concat(fighter.RawFiles))
            {
                Assert.False(rel.StartsWith("Assets/", StringComparison.Ordinal), rel);
                Assert.DoesNotContain('\\', rel);
                var full = Path.Combine(root, "Assets", rel.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(full))
                {
                    missing.Add(fighter.Name + ": " + rel);
                }
                else if (!File.Exists(full + ".meta"))
                {
                    missing.Add(fighter.Name + ": " + rel + ".meta");
                }
            }
        }

        Assert.True(missing.Count == 0, "Missing from the repo:\n" + string.Join("\n", missing));
        Assert.True(PlayerBakeManifest.AllAssets().Count >= 80, "manifest unexpectedly small: " + PlayerBakeManifest.AllAssets().Count);
    }

    [Fact]
    public void Each_fighter_ships_its_attack_and_walk_clips()
    {
        foreach (var fighter in PlayerBakeManifest.All)
        {
            var fbx = fighter.Assets.Where(a => a.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)).ToArray();
            Assert.True(fbx.Length >= 3, fighter.Name + " has only " + fbx.Length + " FBX in the manifest");
            Assert.All(fighter.Assets, a => Assert.StartsWith(fighter.ModelDir + "/", a));
        }
    }

    [Fact]
    public void Runtime_read_metal_rough_textures_are_importer_readable()
    {
        var root = FindRepoRoot();
        var checkedCount = 0;
        foreach (var rel in PlayerBakeManifest.AllAssets().Where(IsTexture))
        {
            var file = Path.GetFileNameWithoutExtension(rel).ToLowerInvariant();
            if (!file.Contains("metal") && !file.Contains("rough"))
            {
                continue;
            }

            var meta = File.ReadAllText(Path.Combine(root, "Assets", rel.Replace('/', Path.DirectorySeparatorChar)) + ".meta");
            Assert.True(meta.Contains("isReadable: 1", StringComparison.Ordinal), rel + " is GetPixels'd at runtime but not Read/Write enabled");
            checkedCount++;
        }

        Assert.True(checkedCount >= 10, "expected the metal/rough maps of every PBR fighter, saw " + checkedCount);
    }

    [Fact]
    public void Sir_Aldric_manifest_paths_match_the_actor()
    {
        var actor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Assets", "Survival", "Unity", "SirAldricMeshyAnimateActor.cs"));
        foreach (var path in new[]
                 {
                     PlayerBakeManifest.SirAldricWalkFbx, PlayerBakeManifest.SirAldricAttackFbx,
                     PlayerBakeManifest.SirAldricSwordFbx, PlayerBakeManifest.SirAldricCapeFbx
                 })
        {
            Assert.Contains("\"" + path + "\"", actor, StringComparison.Ordinal);
        }

        Assert.Contains("mixamo_tex", actor, StringComparison.Ordinal);
    }

    [Fact]
    public void Raw_file_roots_exist_and_exclude_model_folders()
    {
        var root = FindRepoRoot();
        foreach (var dir in PlayerBakeManifest.RawFileRoots.Concat(PlayerBakeManifest.RawFileExcludedDirs))
        {
            Assert.True(Directory.Exists(Path.Combine(root, "Assets", dir)), dir);
        }

        Assert.True(PlayerBakeManifest.IsRawFile("ThemePack/fantasy_kingdom_a/catalog/heroes.json") ||
                    PlayerBakeManifest.IsRawFile("ThemePack/fantasy_kingdom_a/x.json"));
        Assert.False(PlayerBakeManifest.IsRawFile(PlayerBakeManifest.SirAldricWalkFbx));
        Assert.False(PlayerBakeManifest.IsRawFile("ThemePack/fantasy_kingdom_a/art/heroes/3d/lyra/LYRA_rest.fbx"));
    }

    [Theory]
    [InlineData("Assets/Survival/Unity/Survival.Unity.asmdef")]
    [InlineData("Assets/Survival/Runtime/Survival.Domain.asmdef")]
    public void Runtime_asmdefs_have_no_editor_references(string relPath)
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, relPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            path = Directory.GetFiles(Path.GetDirectoryName(path)!, "*.asmdef").Single();
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var rootEl = doc.RootElement;
        foreach (var name in Strings(rootEl, "references").Concat(Strings(rootEl, "precompiledReferences")))
        {
            Assert.DoesNotContain("Editor", name, StringComparison.OrdinalIgnoreCase);
        }

        var include = Strings(rootEl, "includePlatforms").ToArray();
        Assert.True(include.Length == 0, relPath + " is limited to " + string.Join(",", include));
        Assert.Empty(Strings(rootEl, "defineConstraints"));

        var dir = Path.GetDirectoryName(path)!;
        var nested = Directory.GetFiles(dir, "*.asmdef", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(dir, "*.asmref", SearchOption.AllDirectories))
            .Where(p => !string.Equals(Path.GetFullPath(p), Path.GetFullPath(path), StringComparison.Ordinal));
        Assert.Empty(nested);
    }

    [Theory]
    [InlineData("Assets/Survival/Unity")]
    [InlineData("Assets/Survival/Runtime")]
    public void Runtime_code_only_touches_UnityEditor_inside_UNITY_EDITOR_blocks(string relDir)
    {
        var dir = Path.Combine(FindRepoRoot(), relDir.Replace('/', Path.DirectorySeparatorChar));
        var hits = new List<string>();
        foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
        {
            foreach (var (line, text) in PlayerLiveLines(File.ReadAllLines(file)))
            {
                if (Regex.IsMatch(text, @"\bUnityEditor\b|\bAssetDatabase\b|\bEditorApplication\b|\bEditorUtility\b|\bPrefabUtility\b"))
                {
                    hits.Add(Path.GetRelativePath(dir, file) + ":" + line + "  " + text.Trim());
                }
            }
        }

        Assert.True(hits.Count == 0, "Editor API compiled into the player:\n" + string.Join("\n", hits));
    }

    [Fact]
    public void Preprocessor_scanner_tracks_editor_blocks()
    {
        var lines = new[]
        {
            "using UnityEngine; // UnityEditor in a comment",
            "#if UNITY_EDITOR",
            "using UnityEditor;",
            "#else",
            "var live = 1;",
            "#endif",
            "#if !UNITY_EDITOR || FOO",
            "var alsoLive = AssetDatabase;",
            "#endif",
            "#if UNITY_EDITOR && FOO",
            "AssetDatabase.Refresh();",
            "#elif BAR",
            "var bar = 2;",
            "#endif",
            "/* AssetDatabase",
            "   still comment */ var x = 3;"
        };
        var live = PlayerLiveLines(lines).Select(l => l.Text.Trim()).ToArray();
        Assert.Contains("var live = 1;", live);
        Assert.Contains("var alsoLive = AssetDatabase;", live);
        Assert.Contains("var bar = 2;", live);
        Assert.DoesNotContain(live, l => l.Contains("UnityEditor", StringComparison.Ordinal));
        Assert.DoesNotContain(live, l => l.Contains("Refresh", StringComparison.Ordinal));
        Assert.Contains("var x = 3;", live);
    }

    [Fact]
    public void Bake_output_is_gitignored_and_link_xml_preserves_runtime_assemblies()
    {
        var root = FindRepoRoot();
        var ignore = File.ReadAllText(Path.Combine(root, ".gitignore"));
        Assert.Contains("ssets/Survival/PlayerBake/", ignore, StringComparison.Ordinal);
        var link = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "link.xml"));
        Assert.Contains("\"Survival.Domain\"", link, StringComparison.Ordinal);
        Assert.Contains("\"Survival.Unity\"", link, StringComparison.Ordinal);
        var check = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "PlayerBuildMenu.cs"));
        Assert.Contains("IPreprocessBuildWithReport", check, StringComparison.Ordinal);
        Assert.Contains("BuildFailedException", check, StringComparison.Ordinal);
    }

    private static IEnumerable<string> Strings(JsonElement root, string prop)
    {
        if (!root.TryGetProperty(prop, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var e in arr.EnumerateArray())
        {
            yield return e.GetString() ?? "";
        }
    }

    private static bool IsTexture(string rel)
    {
        var ext = Path.GetExtension(rel).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".tga";
    }

    /// <summary>Lines (comments stripped) compiled when UNITY_EDITOR is undefined; other symbols are assumed defined.</summary>
    private static IEnumerable<(int Line, string Text)> PlayerLiveLines(string[] lines)
    {
        // Each frame: is the region live in the player, and has a branch of this #if chain already been taken.
        var stack = new Stack<(bool ParentLive, bool Live, bool Taken)>();
        var live = true;
        var inBlock = false;
        for (var i = 0; i < lines.Length; i++)
        {
            var code = StripComments(lines[i], ref inBlock);
            var t = code.Trim();
            if (t.StartsWith("#if", StringComparison.Ordinal) && (t.Length == 3 || !char.IsLetterOrDigit(t[3])))
            {
                var c = Eval(t.Substring(3));
                stack.Push((live, live && c, c));
                live = live && c;
                continue;
            }

            if (t.StartsWith("#elif", StringComparison.Ordinal))
            {
                var f = stack.Pop();
                var c = !f.Taken && Eval(t.Substring(5));
                stack.Push((f.ParentLive, f.ParentLive && c, f.Taken || c));
                live = f.ParentLive && c;
                continue;
            }

            if (t.StartsWith("#else", StringComparison.Ordinal))
            {
                var f = stack.Pop();
                stack.Push((f.ParentLive, f.ParentLive && !f.Taken, true));
                live = f.ParentLive && !f.Taken;
                continue;
            }

            if (t.StartsWith("#endif", StringComparison.Ordinal))
            {
                live = stack.Pop().ParentLive;
                continue;
            }

            if (live && t.Length > 0 && !t.StartsWith("#", StringComparison.Ordinal))
            {
                yield return (i + 1, code);
            }
        }

        Assert.Empty(stack);
    }

    private static string StripComments(string line, ref bool inBlock)
    {
        var sb = new StringBuilder();
        var inString = false;
        for (var i = 0; i < line.Length; i++)
        {
            if (inBlock)
            {
                if (line[i] == '*' && i + 1 < line.Length && line[i + 1] == '/')
                {
                    inBlock = false;
                    i++;
                }

                continue;
            }

            if (!inString && line[i] == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                break;
            }

            if (!inString && line[i] == '/' && i + 1 < line.Length && line[i + 1] == '*')
            {
                inBlock = true;
                i++;
                continue;
            }

            if (line[i] == '"' && (i == 0 || line[i - 1] != '\\'))
            {
                inString = !inString;
            }

            sb.Append(line[i]);
        }

        return sb.ToString();
    }

    /// <summary>Evaluates a #if expression with UNITY_EDITOR false and every other symbol true.</summary>
    private static bool Eval(string expr)
    {
        var tokens = Regex.Matches(expr, @"\|\||&&|!|\(|\)|==|!=|[A-Za-z_][A-Za-z0-9_]*").Select(m => m.Value).ToList();
        var pos = 0;
        bool Or()
        {
            var v = And();
            while (pos < tokens.Count && tokens[pos] == "||") { pos++; v = And() || v; }
            return v;
        }
        bool And()
        {
            var v = Unary();
            while (pos < tokens.Count && tokens[pos] == "&&") { pos++; v = Unary() && v; }
            return v;
        }
        bool Unary()
        {
            if (tokens[pos] == "!") { pos++; return !Unary(); }
            if (tokens[pos] == "(") { pos++; var v = Or(); pos++; return v; }
            var id = tokens[pos++];
            return id switch { "UNITY_EDITOR" => false, "false" => false, _ => true };
        }

        return Or();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Assets")) && Directory.Exists(Path.Combine(dir.FullName, "tests")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
