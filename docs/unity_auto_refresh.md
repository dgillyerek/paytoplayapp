# Unity auto-refresh after git pulls

`Assets/Survival/Editor/AutoRefreshOnGitChange.cs` (editor-only, `[InitializeOnLoad]`) refreshes the project when
git moves HEAD to another commit (`git checkout -B ... origin/<branch>`, `git pull`, `git reset`, a branch switch),
so you don't have to click into Unity and wait for, or force, an update.

How it works:

- A `FileSystemWatcher` watches the git dir (`HEAD`, `ORIG_HEAD`, `FETCH_HEAD`, `packed-refs`) and `refs/heads/`.
  It only sets a flag. A 2 s `EditorApplication.update` poll compares the HEAD sha as a fallback.
  Worktrees (`.git` file) and packed refs are supported. It only reads files and never runs git.
- When the sha changes it waits 1.5 s after the last change (so a multi-step pull fires once), then calls
  `AssetDatabase.Refresh()`. If any `.cs`/`.asmdef`/`.asmref`/`.rsp` file changed since the last sync it also calls
  `CompilationPipeline.RequestScriptCompilation()`.
- It waits while in Play mode, compiling or importing, and runs as soon as Unity is idle again.
- Console line: `Auto-refreshed after git change to <short sha>`.
- On/off: **Tools > Survival > Auto-Refresh On Git Change** (EditorPrefs, default on). The change applies after the
  next script reload.

## Background throttling (one-time setup, recommended)

Unity slows its editor loop when it is not the focused window, so a pull made while Unity is in the background can
take a few seconds longer to be picked up. The script never changes your preferences. If the refresh feels slow,
set these once:

- **Edit > Preferences > General > Interaction Mode = No Throttling**. With this setting the editor keeps ticking at
  full rate in the background.
- **Edit > Preferences > Asset Pipeline > Auto Refresh = Enabled**. This is Unity's own refresh on focus. The script
  refreshes explicitly either way, so this is a backup.

Limitation: if the OS fully suspends a minimized Unity window, nothing in the editor runs until it is restored.
The refresh then fires right away on the first editor tick.
