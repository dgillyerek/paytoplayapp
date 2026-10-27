using System;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>
    /// Editor-only: after a git checkout/pull/reset moves HEAD to another commit, refresh the AssetDatabase (and
    /// recompile if scripts changed) without the user having to click back into Unity. A FileSystemWatcher on .git
    /// flags changes; a 2 s EditorApplication.update poll of the HEAD sha is the fallback. The refresh fires once,
    /// 1.5 s after the last change, and waits while playing, compiling or importing. See docs/unity_auto_refresh.md.
    /// </summary>
    [InitializeOnLoad]
    static class AutoRefreshOnGitChange
    {
        const string ShaKey = "Survival.AutoRefreshOnGitChange.Sha";   // SessionState: survives domain reloads
        const string SyncedAtKey = "Survival.AutoRefreshOnGitChange.SyncedAtUtcTicks"; // SessionState
        const string EnabledKey = "Survival.AutoRefreshOnGitChange.Enabled"; // EditorPrefs, default on
        const string MenuPath = "Tools/Survival/Auto-Refresh On Git Change";
        const double PollSeconds = 2.0;
        const double DebounceSeconds = 1.5;

        static readonly string ProjectRoot = Path.GetDirectoryName(Application.dataPath);
        static string gitDir;     // .git (or the worktree's git dir)
        static string commonDir;  // where refs/ and packed-refs live (same as gitDir outside worktrees)
        static FileSystemWatcher[] watchers = Array.Empty<FileSystemWatcher>();
        static volatile bool fsDirty;
        static double nextPoll;
        static double fireAt = -1;
        static string pendingSha;

        static AutoRefreshOnGitChange()
        {
            if (!EditorPrefs.GetBool(EnabledKey, true)) return;
            if (!LocateGit(ProjectRoot, out gitDir, out commonDir)) return;
            string sha = ReadHeadSha();
            if (string.IsNullOrEmpty(SessionState.GetString(ShaKey, "")))
            {
                SessionState.SetString(ShaKey, sha ?? "");
                SessionState.SetString(SyncedAtKey, DateTime.UtcNow.Ticks.ToString());
            }
            StartWatchers();
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += StopWatchers;
            EditorApplication.quitting += StopWatchers;
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            bool on = !EditorPrefs.GetBool(EnabledKey, true);
            EditorPrefs.SetBool(EnabledKey, on);
            Debug.Log($"[AutoRefresh] Auto-refresh on git change {(on ? "enabled" : "disabled")} (takes effect after the next script reload).");
        }

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(EnabledKey, true));
            return true;
        }

        static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (fsDirty || now >= nextPoll)
            {
                fsDirty = false;
                nextPoll = now + PollSeconds;
                string sha = ReadHeadSha();
                if (!string.IsNullOrEmpty(sha) && sha != SessionState.GetString(ShaKey, "") && sha != pendingSha)
                {
                    pendingSha = sha;
                    fireAt = now + DebounceSeconds;   // restart the debounce on every new sha
                }
            }
            if (pendingSha == null || now < fireAt) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return; // queued: retried every tick until Unity is idle

            string target = pendingSha;
            pendingSha = null;
            fireAt = -1;
            if (target == SessionState.GetString(ShaKey, "")) return; // e.g. moved away and back during the debounce
            // checkout writes the files before it moves HEAD, so look back to the previous sync (capped at 10 min)
            long.TryParse(SessionState.GetString(SyncedAtKey, "0"), out long ticks);
            var since = new DateTime(Math.Max(ticks, DateTime.UtcNow.AddMinutes(-10).Ticks), DateTimeKind.Utc);
            SessionState.SetString(ShaKey, target);
            SessionState.SetString(SyncedAtKey, DateTime.UtcNow.Ticks.ToString());
            bool scripts = ScriptsChangedSince(since);
            Debug.Log($"Auto-refreshed after git change to {target.Substring(0, Math.Min(7, target.Length))}");
            AssetDatabase.Refresh();
            if (scripts && !EditorApplication.isCompiling) CompilationPipeline.RequestScriptCompilation();
        }

        // ---- git plumbing (plain file reads; never runs git) ----

        static bool LocateGit(string start, out string dir, out string common)
        {
            dir = common = null;
            for (var d = new DirectoryInfo(start); d != null; d = d.Parent)
            {
                string p = Path.Combine(d.FullName, ".git");
                if (Directory.Exists(p)) { dir = p; break; }
                if (File.Exists(p))   // worktree / submodule: "gitdir: <path>"
                {
                    string line = File.ReadAllText(p).Trim();
                    if (!line.StartsWith("gitdir:")) return false;
                    string g = line.Substring(7).Trim();
                    dir = Path.GetFullPath(Path.IsPathRooted(g) ? g : Path.Combine(d.FullName, g));
                    break;
                }
            }
            if (dir == null || !Directory.Exists(dir)) return false;
            common = dir;
            string cd = Path.Combine(dir, "commondir");
            if (File.Exists(cd))
            {
                string c = File.ReadAllText(cd).Trim();
                common = Path.GetFullPath(Path.IsPathRooted(c) ? c : Path.Combine(dir, c));
            }
            return true;
        }

        static string ReadHeadSha()
        {
            try
            {
                string head = ReadShared(Path.Combine(gitDir, "HEAD"))?.Trim();
                if (string.IsNullOrEmpty(head)) return null;
                if (!head.StartsWith("ref:")) return head; // detached HEAD
                string reference = head.Substring(4).Trim();
                foreach (var root in new[] { gitDir, commonDir })
                {
                    string loose = ReadShared(Path.Combine(root, reference.Replace('/', Path.DirectorySeparatorChar)))?.Trim();
                    if (!string.IsNullOrEmpty(loose)) return loose;
                }
                string packed = ReadShared(Path.Combine(commonDir, "packed-refs"));
                if (packed != null)
                    foreach (var raw in packed.Split('\n'))
                    {
                        string l = raw.Trim();
                        if (l.EndsWith(" " + reference)) return l.Substring(0, l.IndexOf(' '));
                    }
                return "unborn:" + reference;
            }
            catch (Exception) { return null; } // file mid-rewrite by git: next poll retries
        }

        static string ReadShared(string path)
        {
            if (!File.Exists(path)) return null;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var r = new StreamReader(fs);
            return r.ReadToEnd();
        }

        static bool ScriptsChangedSince(DateTime utc)
        {
            try
            {
                foreach (var root in new[] { Application.dataPath, Path.Combine(ProjectRoot, "Packages") })
                {
                    if (!Directory.Exists(root)) continue;
                    foreach (var f in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
                    {
                        if (!(f.EndsWith(".cs") || f.EndsWith(".asmdef") || f.EndsWith(".asmref") || f.EndsWith(".rsp"))) continue;
                        if (File.GetLastWriteTimeUtc(f) >= utc) return true;
                    }
                }
            }
            catch (Exception) { return true; }
            return false;
        }

        static void StartWatchers()
        {
            try
            {
                var list = new System.Collections.Generic.List<FileSystemWatcher>();
                // HEAD, ORIG_HEAD, FETCH_HEAD, packed-refs live directly in the git dir; branch refs under refs/heads.
                list.Add(Watch(gitDir, false));
                string heads = Path.Combine(commonDir, "refs", "heads");
                if (Directory.Exists(heads)) list.Add(Watch(heads, true));
                if (commonDir != gitDir) list.Add(Watch(commonDir, false));
                watchers = list.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AutoRefresh] FileSystemWatcher unavailable ({e.Message}); using the {PollSeconds:0} s poll only.");
            }
        }

        static FileSystemWatcher Watch(string dir, bool recursive)
        {
            var w = new FileSystemWatcher(dir)
            {
                IncludeSubdirectories = recursive,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            };
            FileSystemEventHandler h = (_, __) => fsDirty = true; // background thread: only set a flag
            w.Changed += h; w.Created += h; w.Deleted += h;
            w.Renamed += (_, __) => fsDirty = true;
            w.EnableRaisingEvents = true;
            return w;
        }

        static void StopWatchers()
        {
            foreach (var w in watchers) { try { w.EnableRaisingEvents = false; w.Dispose(); } catch (Exception) { } }
            watchers = Array.Empty<FileSystemWatcher>();
        }
    }
}
