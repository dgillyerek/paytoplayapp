# Over-the-air content updates (Theme A, Kingdom Defense)

**Status: HOLD. Do not merge without Derek's OK.** Derek approved building this on Nov 15, 2026.

Phones pick up new **content and data** at launch, with no store update: characters, animations, art and the theme
pack through Unity Addressables, plus a remote config for balance, events, store offers and feature flags. **Code never
travels over the air.** There is no HybridCLR, Lua, IL patching or downloaded assemblies. When a change needs new C#,
raise `min_supported_version` and players are sent to the store.

## Architecture

```
Splash (SurvivalBootstrap) ──► OtaBoot.Run ──► LoadPack / LoadSession ──► age gate ──► Play
                                 │
  1. remote config   GET https://<cdn_host>/config/<platform>/remote_config.json   (5 s timeout)
                     fresh (valid, config_version >= cached) ► cached last-good ► baked Resources/Ota/remote_config.default.json
  2. min version     Application.version < min_supported_version[platform] ► UpdateRequiredScreen (blocks)
                     Android: Play In-App Updates, IMMEDIATE flow; iOS / no Play: store link (parental gate in child mode)
  3. balance         balance.patches applied only if FreePaceGuard accepts the publish-time free_pace_check
                     (PlayerAssets.TextFilter merges RFC 7386 patches into the catalog JSON as it is read)
  4. content         only if cdn_host set, content_version != "", content_api matches, flag ota_content != false:
                     Addressables.InitializeAsync ► CheckForCatalogUpdates ► UpdateCatalogs ► GetDownloadSizeAsync("ota")
                     ► DownloadDependenciesAsync (progress bar + MB on the splash; 20 s stall timeout)
                     ► LoadAssetsAsync<PlayerAssetRegistry>("ota") ► PlayerAssets.SetOverlays
```

* **Where content lives in the game.** The runtime reads every model, clip, texture and raw JSON through
  `PlayerAssets` by `Assets/...` path, backed in a phone by the baked `PlayerAssetRegistry` in Resources. OTA content
  is four more registries (`OTA-Characters`, `OTA-Animations`, `OTA-Art`, `OTA-ThemePack`) shipped in remote Addressables
  bundles. `PlayerAssets` checks them first and falls through to the baked registry, so an update replaces assets path by
  path and anything it lacks comes from the build. The grouping rule is `OtaContentGroups.Classify` (tested).
* **Load path.** The Addressables profile sets `Remote.LoadPath =
  https://{Survival.Unity.Ota.OtaHost.CdnHost}/content/[BuildTarget]/{Survival.Unity.Ota.OtaHost.ContentVersion}`.
  Curly braces are evaluated on the phone: the host comes from `Assets/Survival/Resources/Ota/ota_settings.json` and the
  folder comes from the remote config's `content_version`. Hosting is swappable without a rebuild of the content code;
  changing `cdn_host` needs a new store build because it is baked.
* **Catalog.** There is one remote catalog per content folder, with player version fixed to `ota` (`catalog_ota.hash`),
  and "only update catalogs manually" is on, so nothing touches the network before `OtaBoot` decides to.
* **Code:**
  - `Assets/Survival/Runtime/Ota/` is the domain layer, tested by `dotnet test`. It holds `RemoteConfigParser`
    (strict schema), `RemoteConfigSelector`, `MinVersionGate`/`AppVersion`, `FreePaceLock`/`FreePaceGuard`/`RemoteBalance`,
    `JsonPatch` (merge patch, canonical JSON, SHA-256), `OtaContentGroups`, `OtaSettings` and `OtaBootProgress`.
  - `Assets/Survival/Unity/Ota/` holds `OtaBoot`, `RemoteConfigClient`, `OtaContentUpdater` (Addressables, behind the
    `SURVIVAL_ADDRESSABLES` define), `InAppUpdate` (JNI to Play app-update), `UpdateRequiredScreen`, `OtaHost` and
    `OtaRuntime`. Gameplay reads flags, event schedules and offer presentation from `OtaRuntime`.
  - `Assets/Survival/Editor/Ota/` holds `OtaAddressablesSetup` (groups, profile, catalog settings; idempotent),
    `OtaContentBuild` (bake ► split ► Addressables build ► `content_manifest.json`) and `AndroidInAppUpdateGradle`, which
    adds `com.google.android.play:app-update:2.1.0` to the generated Gradle project.
  - `CiAndroidBuild` calls `OtaContentBuild.PrepareGroups()` so every player build's catalog knows the OTA groups
    (Addressables "build with player" is on).
  - Package: `com.unity.addressables` 2.10.3, the version released for Unity 6000.3.

## Remote config (schema `paytoplay.remote_config`, v1)

```json
{
  "schema": "paytoplay.remote_config",
  "schema_version": 1,
  "config_version": 7,
  "app_id": "com.paytoplay.fantasykingdoma",
  "content_version": "2026.11.20-1",
  "content_api": 1,
  "min_supported_version": { "android": "0.1.40", "ios": "0.1.40" },
  "store_links": { "android": "https://play.google.com/store/apps/details?id=com.paytoplay.fantasykingdoma",
                   "ios": "https://apps.apple.com/app/id<APP_STORE_ID>" },
  "feature_flags": { "ota_content": true },
  "events": [ { "id": "event.harvest", "enabled": true, "starts_at": "2026-12-01T00:00:00Z", "ends_at": "2026-12-08T00:00:00Z" } ],
  "store_offers": [ { "offer": "offer.power_t3", "enabled": true, "badge": "New", "sort": 1 } ],
  "balance": { "patches": { "runner_levels.json": { "...": "RFC 7386 merge patch" } },
               "free_pace_check": { "balance_sha256": "…", "wall": 31, "cleared_day": { "10": 3.0, "31": 13.0, "50": 828.67 } } }
}
```

Validation is strict, and an invalid document is rejected whole (the cached or baked copy is used):

* unknown keys are rejected; `_`-prefixed keys are comments;
* `schema_version` must be ≤ 1, and `app_id` must be this app;
* `content_version` must be 0–64 characters from `[A-Za-z0-9._-]` (it becomes a URL segment);
* store links must be https, and dates must be ISO UTC with `Z`;
* the document is at most 256 KB.

`config_version` must rise on every publish. Phones never move to a lower one than they cached, which protects against a
stale CDN edge.

* **Events** only schedule events the build ships (`events.json`). Rewards stay baked.
* **Store offers** only toggle, badge, sort or time-window offers the build ships. Price and contents stay baked and
  server-validated (Play Console and Neon `/v1/iap/validate`), so remote config cannot change what a purchase grants.
* **Balance** patches may touch only the files the free-pace model reads: `runner_levels.json`, `campaign.json`,
  `base_economy.json`, `gifts.json`, `quests.json`, `stores.json` and `power_packs.json`. `runner_balance.json` (the
  model's own assumptions) cannot be patched.

### Locked free pace

The free, no-power-up pace is locked: the wall at L31, L31 cleared on day 13, and L50 around day 829, each within 3% or
one session. A phone cannot run the sim, so balance patches are checked when they are published:

1. `scripts/ota/check_remote_balance.sh ota/remote_config.json` validates the document, applies the patches to the
   shipped catalog, and runs the same economy and runner sim as `RunnerFreePaceTests`
   (`RemoteBalancePaceTests.Candidate_config_keeps_the_locked_free_pace`). It **fails** if the free pace moves. On a pass
   it stamps `balance.free_pace_check`, which holds the SHA-256 of the canonical patches and the sim's wall and days.
2. On the phone, `FreePaceGuard` applies patches only when that hash matches the patches received and the stamped wall
   and days are within the lock. Unchecked, edited or pace-moving patches are dropped, and the baked balance stays.
   Flags, events and offers still apply.

If a remote change needs more enemy strength to hold the pace, re-tune `runner_levels.json` `level_tweaks.foe_hp` in
the same patch. Do not edit the targets.

## Publishing a content update

1. Make the art or data change in the project as usual, then run **Survival/Player Build/Bake Player Assets** to check
   it.
2. Build the content: either use **Survival/OTA/Build Content (active build target)** in the Editor, or (preferred) run
   the GitHub workflow **OTA content** (`ota-content.yml`) with a new `content_version`, e.g. `2026.11.20-1`. The workflow
   builds Android content and uploads it to `content/Android/<content_version>/`. Locally, run
   `scripts/ota/publish_content.sh Android 2026.11.20-1` with the `OTA_S3_*` variables set. Content folders are immutable
   and are never overwritten. After a catalog upload, flush the Spaces CDN (see Hosting below) so edges drop any previous
   catalog immediately instead of waiting out `max-age=60`.
3. Go live: in `ota/remote_config.json`, set `content_version` to the new folder and raise `config_version`. Then
   either run the workflow with `publish_config` ticked, or run `scripts/ota/publish_config.sh ota/remote_config.json`
   locally. That script runs the free-pace check, refuses a non-increasing `config_version`, checks that the content
   folder exists, writes `config/<platform>/history/config_v<N>.json`, and then writes the live
   `config/<platform>/remote_config.json` (cache 60 s).
4. Phones load it on their next launch, showing "Downloading new content x / y MB" on the splash.

What content can change: models, clips, textures, sprites, audio, theme-pack JSON and catalog JSON, using **component
types the shipped app already has**. New scripts, new MonoBehaviours or new Unity modules need a store build, plus a
`min_supported_version` bump if old installs cannot run the new content. Raise `content_api`, both baked in
`ota_settings.json` and in the config, when the content format itself changes. Older builds then stay on their bundled
content instead of loading content they cannot read.

## Rollback

To roll back, re-point and re-publish. Nothing is deleted.

* **Content:** set `content_version` back to an older folder (for example `2026.11.15-1`), or to `""` for the content
  baked into the build. Then raise `config_version` and publish. The old folder's catalog and bundles are still on the
  bucket, and phones that cached them do not even download.
* **Config:** republish an old `config/<platform>/history/config_v<N>.json` with a higher `config_version`.
* **Kill switch:** `"feature_flags": { "ota_content": false }` stops all content downloads, so phones run on bundled
  content.

## Offline and failure behaviour

| Situation | Result |
| --- | --- |
| `cdn_host` empty | No network at all. Baked content and baked config are used, and the game plays exactly as before. |
| `cdn_host` set, baked `content_version` empty (this PR's default host is `ota.brightmoatstudios.com`) | Config is fetched at boot. Content stays bundled until a remote config with a `content_version` is published. A DNS miss or timeout falls back to the last-good, then baked, config. |
| Offline, no cache | Baked config and bundled content. |
| Offline, earlier download cached | Cached config, cached catalog, cached bundles. The OTA content still shows (the download size is 0, so it loads from the device). |
| Config 404, timeout, invalid, or older than cached | Cached, then baked, config. Logged. |
| Catalog check timeout (8 s) or failure | Current cached catalog is used. Its content shows if it is fully on the device, otherwise bundled content. |
| Download stalls 20 s or fails | Bundled content this launch. The download resumes next launch. |
| Overlay pack fails to load | Overlays and patches are dropped, and the game reloads bundled content. |
| Below `min_supported_version` (fresh or cached config) | Blocked by the update screen. This is the only blocking case. |

## Kid safety (COPPA, Google Play Families)

* Plain HTTPS GETs to fixed URLs: no query strings, no custom headers, and no device, install, advertising or account
  identifiers. Every phone requests the same URLs. `OtaTests.Ota_downloads_data_only_and_sends_no_identifiers` scans
  the OTA sources for identifiers, header writes, reflection and script runtimes.
* No analytics, crash or third-party SDK is added. Play `app-update` declares no permissions and no AD_ID.
  `AndroidFamiliesManifest` AD_ID stripping is unchanged, and the CI bundle check still runs.
* On the CDN, turn off anything that sets cookies or fingerprints visitors on the OTA domain. Do not enable DigitalOcean
  or other analytics, RUM, or cookie injection on `ota.brightmoatstudios.com`. Keep logs at the default and do not use
  them for user analytics. Public file listing on the Space stays off; only objects uploaded with `ACL public-read` are
  fetchable.
* The update screen's store link sits behind the existing parental gate in child mode (Apple Kids and Families: no link
  out of the app without a grown-up). Play's in-app update UI is Play's own and needs no link.

## Hosting: DigitalOcean Spaces (nyc3) + public CDN domain

OTA files live in the DigitalOcean Space **`kingdomdefense-ota`** in **nyc3**. The scripts use the standard AWS CLI
against the S3-compatible endpoint `https://nyc3.digitaloceanspaces.com`. Phones load over HTTPS from the managed CDN
host **`ota.brightmoatstudios.com`**, baked as `cdn_host` in `Assets/Survival/Resources/Ota/ota_settings.json` (no
scheme, no path). The load path is `https://<cdn_host>/content/[BuildTarget]/{contentVersion}` and the remote config is
`https://<cdn_host>/config/<platform>/remote_config.json`.

**DNS fallback.** The domain's nameservers were moved to DigitalOcean, so `ota.brightmoatstudios.com` may take up to
about a day to resolve everywhere. Until it does, the same public objects are also at
`https://kingdomdefense-ota.nyc3.cdn.digitaloceanspaces.com/...`. Do not bake that fallback host into a store build;
it is only for checks and temporary test builds. After DNS has settled, keep `cdn_host` on `ota.brightmoatstudios.com`.

**Public-read uploads.** The bucket's file listing is restricted (anonymous `List` is off). Every object must be
uploaded with `ACL public-read` so phones can GET it over the CDN. `scripts/ota/_env.sh` adds `--acl public-read` on
Spaces uploads. Cache-Control is `public, max-age=31536000, immutable` for hashed bundles and history copies, and
`public, max-age=60` for catalogs, `.hash` files, `content_manifest.json`, and the live remote config.

**CDN cache purge.** After publishing a new catalog or a live config, flush the Spaces CDN so edges drop the previous
files immediately instead of waiting out the 60-second max-age:

```
doctl compute cdn list
doctl compute cdn flush <cdn-id> --files /content/Android/<content_version>/* /config/*
```

`publish_content.sh` and `publish_config.sh` print a reminder with the paths just uploaded. A full flush (`doctl
compute cdn flush <cdn-id>` with no `--files`) is fine if the id is handy and the Space only holds OTA objects.

The alternative is AWS S3 + CloudFront: set `OTA_S3_ENDPOINT=aws` and use an IAM key with the same secret names, plus
optionally `OTA_CLOUDFRONT_DISTRIBUTION_ID` for config invalidation. Egress is billed per GB there. That path does not
add a public-read ACL.

Bucket layout:

```
config/android/remote_config.json            live, Cache-Control max-age=60
config/android/history/config_v<N>.json      every published version
config/ios/...
content/Android/<content_version>/catalog_ota.{bin|json,hash}, *.bundle, content_manifest.json
content/iOS/<content_version>/...
```

## What is already set up, and what is still needed

Already in place (Nov 16, 2026):

1. **DigitalOcean Space** `kingdomdefense-ota` in **nyc3**, S3 endpoint `https://nyc3.digitaloceanspaces.com`.
2. **CDN host** `ota.brightmoatstudios.com` (managed Let's Encrypt cert), baked as `cdn_host` in
   `Assets/Survival/Resources/Ota/ota_settings.json`. Fallback while DNS propagates:
   `kingdomdefense-ota.nyc3.cdn.digitaloceanspaces.com`.
3. **GitHub repo secrets**, from a Spaces key limited to that bucket (read/write/delete):
   - `OTA_S3_ACCESS_KEY_ID`
   - `OTA_S3_SECRET_ACCESS_KEY`
   - `OTA_S3_BUCKET`
   - `OTA_S3_ENDPOINT`

   The existing `UNITY_EMAIL` and `UNITY_PASSWORD` secrets are reused.

Still needed:

4. **A Unity license secret** (`UNITY_LICENSE` for Personal, or `UNITY_SERIAL` for Plus/Pro) so the OTA content
   workflow can build Addressables in CI. Until that is set, content can be built on a machine with the Editor and
   uploaded with `scripts/ota/publish_content.sh`.
5. **The workflow switched on.** Copy `ci/github-workflows/ota-content.yml` to `.github/workflows/ota-content.yml` (see
   `ci/github-workflows/README.md`), or approve `gh auth refresh -s workflow` for the bot.
6. **Play Console (in-app updates).** In-app updates only work for installs **from Google Play** (any track, including
   internal testing), when a **higher versionCode** is published on that track for the user, and with the **same
   signing key**. Nothing needs switching on in Play Console. To test it, use internal app sharing or an internal-testing
   track with two versionCodes. Sideloaded and phone-build APKs fall back to the store link.
7. **iOS App Store id.** Once the App Store Connect record exists, replace `id0000000000` in `store_links.ios` (both
   `ota/remote_config.json` and `Assets/Survival/Resources/Ota/remote_config.default.json`). iOS content builds also need
   an iOS build target, which the workflow does not build yet.

## Not verified yet

This code was written on a box with no Unity editor. The domain layer is covered by `dotnet test`. The Unity and
Editor code (Addressables, JNI, UI) compiles by inspection against the Addressables 2.10.3 source only. Before merging,
check these in the Editor:

1. Open the project and let Unity resolve `com.unity.addressables` (updates `Packages/packages-lock.json`).
2. Run **Survival/OTA/Set Up Addressables** and commit `Assets/AddressableAssetsData`.
3. Run **Survival/OTA/Build Content**, upload it (or serve `ServerData/` locally), and check the splash progress bar,
   offline fallback, and a min-version block. `cdn_host` is already set to `ota.brightmoatstudios.com`.
4. Build an `.aab` and confirm the Gradle hook added `app-update` and the AD_ID check still passes.
