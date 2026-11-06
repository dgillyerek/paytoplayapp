# Android phone builds

GitHub builds the Survival game as an Android APK in the cloud (`.github/workflows/android-build.yml`). You can install
the result straight from your phone, with no computer and no USB cable.

## When a build runs

- **Every push to `main`** (including merging a PR). The result goes to the `android-latest` prerelease.
- **Any branch, on demand:** on GitHub open **Actions → Android phone build → Run workflow**, type the branch name, and
  press **Run workflow**. This works in a phone browser too (use "Desktop site" if the button is hidden).
- **A pull request labelled `phone-build`:** add the label (this works in the GitHub phone app). The PR then rebuilds
  on every new commit until you remove the label.

A first build takes roughly an hour, because Unity imports everything from scratch. Later builds reuse main's cached
import and are faster. Builds are free because the repo is public.

## Installing on the phone

1. Open the download link in Chrome on the phone:
   - main: https://github.com/dgillyerek/paytoplayapp/releases/download/android-latest/paytoplayapp-latest.apk
   - a branch, e.g. `game-audio`: https://github.com/dgillyerek/paytoplayapp/releases/download/android-game-audio/paytoplayapp-game-audio-latest.apk
     (the branch name is lower-cased, and `/` or `_` become `-`)
   - or open https://github.com/dgillyerek/paytoplayapp/releases and tap the `.apk` under the build you want.
2. Tap the downloaded file. The first time, Android asks you to allow **Chrome** to install unknown apps. Turn that on
   and go back.
3. Tap **Install** (or **Update**). Play Protect may warn that the app is from an unknown developer. Choose
   **Install anyway**.

The link always points at the newest build, so bookmark it. Each build is signed with the same key and gets a higher
version number, so a new build installs over the old one and keeps your save. Two cases need an uninstall first:

- A copy you installed from Unity over USB is signed with a different key. Uninstall it once before the first cloud
  build.
- Going *back* to an older build, for example from a newer `main` build to an older branch build. Android refuses
  downgrades.

## One-time setup (Unity license)

The Android signing key is already stored in the repo secrets (`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`,
`ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS`). Never replace these, or updates stop installing over the old app.

Unity needs your account to build in the cloud. Add these in GitHub under **Settings → Secrets and variables → Actions
→ New repository secret**:

| Secret | Value |
| --- | --- |
| `UNITY_EMAIL` | the email you sign in to Unity with |
| `UNITY_PASSWORD` | your Unity password |
| `UNITY_LICENSE` | **Unity Personal (free):** the full text of your `Unity_lic.ulf` file |
| `UNITY_SERIAL` | **Unity Plus/Pro only, instead of `UNITY_LICENSE`:** your serial key |

How to get `Unity_lic.ulf` (Personal): in Unity Hub open **Preferences → Licenses → Add → Get a free personal license**.
Do this even if a license already shows. Then open the file and copy all of it:

- Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
- Mac: `/Library/Application Support/Unity/Unity_lic.ulf`

Details: https://game.ci/docs/github/activation

Then run the workflow once (Actions → Android phone build → Run workflow, branch `main`). After that, every push to
`main` builds by itself.

## Good to know

- The repo is public, so anyone with the link can download the APKs.
- Builds are release builds by default. Tick **Development build** in Run workflow when you want the Unity development
  overlay and extra logging.
- The build runs `Survival.Editor.CiAndroidBuild.Build`. It does the same as **Survival/Player Build/Build Android
  Development APK**: it bakes the player assets, refuses a stale bake, and builds an IL2CPP ARM64 APK.
- Each run is also kept for 14 days as a zipped workflow artifact. On a phone, use the release link instead.
