# Analytics & crash vendors (Families / COPPA)

**Status: HOLD — Derek picks the vendor + accounts before any SDK package is added.**

This app’s soft-launch audience includes children under 13 (Canada, Philippines, Australia, New Zealand) and must follow [Google Play Families Policies](https://support.google.com/googleplay/android-developer/answer/9893335) and [Families data practices](https://support.google.com/googleplay/android-developer/answer/11043825). Code today ships a vendor-agnostic `IAnalytics` / `ICrashReporter` with a no-op sink, hard-gated off for **under-13 and unknown age** (`KidGatedAnalytics` / `KidGatedCrashReporter`). `child_safety.json` keeps `telemetry.*_vendor = "none"` and child analytics/crash = `off`.

## Play Families rules that apply to every candidate

- **No AAID / advertising ID, SIM/Build serial, BSSID/MAC/SSID, IMEI/IMSI from children or unknown age.** Prefer [App Set ID](https://support.google.com/googleplay/android-developer/answer/11043825) for analytics when an install-scoped id is required.
- Apps that **solely** target children (API 33+): do **not** declare `com.google.android.gms.permission.AD_ID` (or remove it from the merged manifest).
- Mixed audience: default **off** at cold start; only enable stronger collection after a neutral age screen says the user is not a child.
- **Self-Certified Ads SDK list is for ads SDKs only** ([Participate](https://support.google.com/googleplay/android-developer/answer/12955712), [Policy](https://support.google.com/googleplay/android-developer/answer/12918983)). Analytics/crash SDKs are **not** on that list; they still need their own child-directed ToS + config review, and must not pull in a non-certified ads SDK.
- This soft launch is **not** planning child ads; if ads appear later, only self-certified ads SDK versions from Google’s list may serve children / unknown age.

## Candidate SDKs (no package added yet)

### 1. Firebase Analytics + Crashlytics

| | |
|---|---|
| **Role** | Product analytics + crash reporting (Google) |
| **Families fit** | Usable if AD_ID / personalization are disabled and collection stays off for children / unknown age. Not an ads SDK; still subject to Families data practices. |
| **Kid / Families config (Android)** | In `AndroidManifest.xml` `<application>`: `google_analytics_adid_collection_enabled=false`; `google_analytics_default_allow_ad_personalization_signals=false` (or `GOOGLE_ANALYTICS_DEFAULT_ALLOW_AD_PERSONALIZATION_SIGNALS` on iOS); keep `firebase_analytics_collection_enabled=false` until age gate says 13+. Optionally exclude `play-services-ads-identifier`. Crashlytics: `firebase_crashlytics_collection_enabled=false` by default; enable only for 13+. |
| **Sources** | [Configure Analytics data collection](https://firebase.google.com/docs/analytics/configure-data-collection); [Families data practices](https://support.google.com/googleplay/android-developer/answer/11043825); Crashlytics collection flags in Firebase Android reference. |

### 2. Sentry (crash / error monitoring)

| | |
|---|---|
| **Role** | Crash / error monitoring (optional performance) |
| **Families fit** | **Caution.** Sentry docs say the SDK is not an ads SDK and does not require IDFA ([Mobile privacy](https://docs.sentry.io/security-legal-pii/security/mobile-privacy/)). However, developers have reported **Play Console rejections** classifying Sentry as disallowed under Families ([forum](https://forum.sentry.io/t/sentry-sdk-not-allowed-in-google-play-families-program/13339), [sentry-java#1340](https://github.com/getsentry/sentry-java/issues/1340)). Confirm current Play review posture with counsel / a closed testing track before adopting. |
| **Kid config** | `sendDefaultPii = false`; do not set user email/IP; disable screenshots / view hierarchy; keep SDK init behind the 13+ gate (our `KidGatedCrashReporter` already no-ops under-13 / unknown). |
| **Sources** | [Sentry mobile privacy](https://docs.sentry.io/security-legal-pii/security/mobile-privacy/); Families policy links above. |

### 3. GameAnalytics

| | |
|---|---|
| **Role** | Game-specific analytics |
| **Families fit** | Possible for **13+ only** with advertising-ID tracking off. Not on the Families ads self-cert list (N/A unless it embeds ads). Must not read IMEI / AAID for children. |
| **Kid config** | Before init: `EnableAdvertisingIdTracking(false)`; do **not** call `GAImei.readImei()` — use `doNotReadImei()` on Android. Prefer keeping `SetEnabledEventSubmission(false)` until age gate = 13+. Review GA’s current child / COPPA terms with counsel. |
| **Sources** | [GameAnalytics Unity configuration](https://docs.gameanalytics.com/event-tracking-and-integrations/sdks-and-collection-api/game-engine-sdks/unity/configuration); [Android SDK](https://docs.gameanalytics.com/event-tracking-and-integrations/sdks-and-collection-api/platform-sdks/android); Families data practices. |

### 4. Unity Analytics (UGS)

| | |
|---|---|
| **Role** | Unity Gaming Services analytics |
| **Families fit** | Unity’s privacy FAQ: designate the project as a **Child App** so anonymized IDs are not used to track across other developers’ apps; note that Analytics may still collect IP and, if Unity Ads is also enabled, IDFA — **do not enable Unity Ads for child traffic** unless on the self-certified ads list and correctly age-gated. |
| **Kid config** | Mark Child App in Dashboard / Editor; keep data collection disabled until 13+; strip `AD_ID` permission from the merged manifest; do not enable Ads personalization. |
| **Sources** | [Unity Privacy FAQ](https://support.unity.com/hc/en-us/articles/4408924899348--New-Privacy-FAQ); [Unity Analytics Google data safety](https://docs.unity.com/en-us/analytics/privacy-and-consent/google-data-safety); Families data practices. |

## Ads SDK list (context only)

If Theme A later shows ads to children / unknown age, only versions on Google’s [Families Self-Certified Ads SDK Program](https://support.google.com/googleplay/android-developer/answer/12955712) may be used for those users (e.g. AdMob `play-services-ads` 19.0.0+, Unity Ads 4.0.1+, … — **re-check the live page before every release**; versions change). Analytics vendors above are not substitutes for that list.

## What this repo does today

- Domain: `IAnalytics`, `ICrashReporter`, `NoOp*`, `FunnelEvents` schema, `KidGatedAnalytics` / `KidGatedCrashReporter`.
- Unity: `Survival.Unity.Analytics.SurvivalAnalytics` → kid gate → `KidSafety.Telemetry` (`PolicyTelemetry` + no-op vendor).
- Funnel events: `first_open`, `age_gate_shown` / `age_gate_completed` (**band only**), tutorial step start/complete, quest step complete, `level_up`, `first_battle`, `store_view`, `iap_attempt` / `success` / `fail`, `return_d1` / `return_d7`.
- **No vendor SDK package, no ProjectSettings / workflow edits.**

## Derek decisions needed

1. Analytics vendor (Firebase / GameAnalytics / Unity Analytics / other / stay none).
2. Crash vendor (Firebase Crashlytics / Sentry / other / stay none) — confirm Families review risk for Sentry.
3. Create the vendor accounts + app keys (and Firebase/Unity projects if chosen).
4. Whether adult (13+) analytics should move from `off` → `limited` or `full` in `child_safety.json` once a vendor is wired.
5. Counsel sign-off on Data safety form answers and privacy policy text for the chosen SDKs.
