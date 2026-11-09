# Android release builds (Google Play)

`.github/workflows/android-build.yml` builds **Kingdom Defense** (`com.paytoplay.fantasykingdoma`, Theme A flavor
`fantasy_kingdom_a`) as a signed Android App Bundle (`.aab`) for Google Play.

## What a build is

| | |
| --- | --- |
| Trigger | every push to `main`; or **Actions → Android build → Run workflow** with format `aab` on any branch |
| Flavor | `-flavor fantasy_kingdom_a` → `FlavorBuildSettings.TryApplyFlavor` (same as menu **Survival/Use Theme A Flavor**): product name, company `PayToPlay`, bundle id, scenes, icons |
| Scripting | IL2CPP, ARM64 only |
| Target API | 36 (Android 16), required by Google Play for new apps and updates since 2026-08-31. Min API 26 |
| Signing | the upload key from repo secrets `ANDROID_KEYSTORE_BASE64` / `ANDROID_KEYSTORE_PASS` / `ANDROID_KEYALIAS_NAME` / `ANDROID_KEYALIAS_PASS`. The build fails rather than fall back to Unity's debug key |
| versionCode | `ANDROID_VERSION_CODE_OFFSET` (repo variable, default 0) + the workflow run number, so it rises on every run |
| versionName | `ANDROID_VERSION_NAME_PREFIX` (repo variable, default `0.1`) + `.` + run number |
| Output | workflow artifact `kingdomdefense-<branch>-<sha>-<versionCode>-aab`, kept 30 days |

Nothing secret is committed: `*.keystore`, `*.jks` and `*.aab` are gitignored, ProjectSettings has
`androidUseCustomKeystore: 0` with an empty keystore path, and the build clears the keystore settings again when it
finishes. For a local command-line build the same values can come from environment variables `ANDROID_KEYSTORE_NAME`
(path to the keystore), `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS`.

## Uploading to Play

1. Open the finished run, download the `…-aab` artifact and unzip it.
2. Play Console → Kingdom Defense → **Testing → Internal testing** (or Closed / Production) → **Create new release** →
   upload the `.aab`.
3. Play rejects a versionCode it has already seen. Every run gets a new one; if this workflow is ever renamed or
   recreated, set the repo variable `ANDROID_VERSION_CODE_OFFSET` above the last uploaded versionCode first.

## One-time setup (Derek)

**GitHub (Settings of `dgillyerek/paytoplayapp`)**

0. Put the workflow live: it is staged at `ci/github-workflows/android-build.yml` because the bot's GitHub login
   lacks the `workflow` permission. Copy it to `.github/workflows/android-build.yml` on GitHub, or approve the bot's
   `gh auth refresh -s workflow` code so it can push it (`ci/github-workflows/README.md`).
1. *Actions → General → Actions permissions*: allow all actions, or at least GitHub-authored actions plus
   `game-ci/unity-builder@*`, `jlumbroso/free-disk-space@*`.
2. *Actions → General → Workflow permissions*: **Read and write permissions** (the phone-APK path publishes GitHub
   prereleases; the `.aab` path only needs read).
3. *Secrets and variables → Actions → Secrets*:
   - `UNITY_EMAIL`, `UNITY_PASSWORD`: already set.
   - `UNITY_LICENSE` (Unity Personal: full text of `Unity_lic.ulf`) **or** `UNITY_SERIAL` (Unity Plus/Pro serial).
     **Missing: no build can run until one of these is added.** How to get the `.ulf`: `docs/android-phone-builds.md`.
   - `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS`: already set
     (the CI key made for phone builds on 2026-10-26). It becomes the Play **upload key** at the first upload. To use your
     own upload key instead, replace all four **before** the first Play upload: `base64 -w0 upload.keystore` for
     `ANDROID_KEYSTORE_BASE64`. Keep an offline backup of whichever keystore and passwords you use.
4. *Variables* (optional): `ANDROID_VERSION_CODE_OFFSET`, `ANDROID_VERSION_NAME_PREFIX` (e.g. `1.0` for launch).

**Google Play Console**

1. A Google Play developer account (one-time US$25).
2. **Create app**: name *Kingdom Defense*, default language, Game, Free, accept the declarations. The package name is
   set by the first upload and can never change: `com.paytoplay.fantasykingdoma`.
3. **Play App Signing**: keep the default (Google-generated app signing key). The CI keystore above is the upload key.
4. Store listing art. Present: hi-res icon 512×512 at `store-listing/fantasy_kingdom_a/play_store_icon_512.png`
   (Direction-5 store icon, Derek PASS 2026-09-15). **Missing**: feature graphic 1024×500, at least 2 phone
   screenshots (portrait 1080×1920 works), short and full description, privacy policy URL, content rating
   questionnaire, Data safety form, target audience, ads declaration.
5. Before the first production release on a new personal developer account, Play requires a closed test with at least
   12 testers for 14 days.

## App icons

From the locked Direction-5 store icon (`design/survival-theme-a-fantasy/direction-5/05_store_icon_fantasy_kingdom.png`),
committed under `Assets/ThemePack/fantasy_kingdom_a/art/store/`:

- `flavor_icon.png` (1024): default icon and Android legacy slots;
- `flavor_icon_round.png` (1024, circle-cut): Android round slots;
- `flavor_icon_adaptive_background.png` / `flavor_icon_adaptive_foreground.png` (432): Android adaptive layers. The
  foreground is the full icon art scaled into the 66 dp safe zone with its sky gradient carried out to the bleed; the
  background is the same gradient. It is not a separate cut-out castle layer, so the launcher parallax moves the whole
  picture. A layered source (castle on transparency + sky) from Design would give a true adaptive icon.

iOS icon slots are still empty (iOS is not in scope yet).
