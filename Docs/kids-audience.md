# Audience switch: 13+ (teen_plus) and Families

**Status:** 13+ is the soft-launch audience (Derek, 2026-11-16). The soft launch is in Canada, the Philippines, Australia and New Zealand. The under-13 / Families code stays in the build, switched off. Not legal advice; counsel reviews before ship.

## The switch

| Where | Setting | Default |
|---|---|---|
| Client | `Assets/ThemePack/fantasy_kingdom_a/kids/child_safety.json` → `"audience_mode": "teen_plus" \| "families"` | `teen_plus` (it is also the code fallback when the file is missing) |
| Server (`alliance` Neon Function) | env `AUDIENCE_MODE=teen_plus \| families` (`Backend/alliance/src/audience.mjs`) | `teen_plus` (it is also the fallback for unknown values) |

Keep the two in step. The client reads its value at boot (`KidSafety.Config.Audience`); the server reports its own on `GET /v1/health` and `GET /v1/account` (`audienceMode`).

## What each mode does

| | **teen_plus** (13+, default) | **families** (mixed audience, kept intact) |
|---|---|---|
| First-launch age screen | The same neutral screen (birth month and year, no default, no "13+" hint). Asked once per install; the answer is stored signed and can't be re-asked. Back and force-close re-ask. | Same |
| Under 13 | **Blocked.** A polite "made for players aged 13 and over" screen with no buttons replaces the game (`AudienceBlockScreen`). Data collected by mistake is deleted (see below), and only the under-13 record is kept so the block holds. | Child mode: child rules from `child_safety.json` `child` |
| Edited / removed stored answer | Blocked (nothing is deleted, because the age isn't known to be under 13) | Child mode |
| 13+ | `adult` rules: purchases, free-text guild chat with filter, report, block and moderation, sign-in, cloud save, and analytics/crash once a vendor is picked | Same |
| Parental consent (`IParentalConsentProvider`, `IParentalConsent`) | Hook kept in code but **off**. `ConsentGate` returns `Blocked` for anything consent-gated; the Settings consent row is hidden. | On: under-13 sign-in and cloud save need granted consent; purchases go through the parental gate |
| Age correction | Settings → **Update my age** asks the same neutral question. An under-13 answer lowers the band (it can never be raised), blocks play and runs the deletion. A 13+ answer changes nothing. | Not offered (age resets only by deleting the account) |
| Purchases (`AgeBandPurchaseGate`) | 13plus buys. under13 is declined (`iap.audience_13plus`) with no consent step. Unknown is still declined (`iap.age_unknown`), but unknown only exists before the age screen is answered. | 13plus buys; under13 → parental gate; unknown declined |
| Server `/v1/iap/validate` | `403 audience_13plus_only` for under13 accounts | Unchanged (no server age gate) |
| Server `/v1/save` PUT | `403 audience_13plus_only` for under13 accounts | under13 needs `consentGranted` |
| Server `POST /v1/account/age_band` `under13` | Deletes the account's data (`deletePlayer`) and keeps only `age_band='under13'` on the scrubbed row | Stores the band (sticky) |
| Server chat for under13 | Always `off` | `off` or `presets` |
| Analytics / crash | `KidGatedAnalytics` / `KidGatedCrashReporter` send for 13plus only, and only once a vendor is set (`telemetry.*_vendor` is still `"none"`, so today nothing is sent). Blocked and unknown never send. | Same gate |
| AD_ID | `AndroidFamiliesManifest` still strips `com.google.android.gms.permission.AD_ID`. It's left as is because there is no ad SDK; revisit if an ads or attribution SDK is ever added in 13+ mode. | Must stay stripped |

### Under-13 deletion (Product checklist A2)

`AudienceBlock.Enforce` reuses the in-app account deletion (`AccountDeletion`):
1. If this install ever signed in (`alliance.device` exists), the server account is deleted first (`POST /v1/account/delete`). The block screen signs in with the stored device id just to do this.
2. Then the device is wiped (`SettingsScreen.WipeDevice`: PlayerPrefs plus the persistent data and cache folders).
3. Then the signed under-13 age record is written back. It is the only thing kept.

If the server can't be reached, nothing local is wiped. The block stays, and the next launch retries. While the block screen is up, a pause or quit wipes again, so an autosave can't write data back.

## 13+ guild chat (Product checklist A3)

- **Free text** for 13+ only (`effectiveChatMode`). Report and block were already in place (`/v1/chat/report`, `/v1/chat/block`).
- **Filter**: `kids/chat_filter.json` is shared by the client (`ChatFilter`, which masks what is shown) and the server (`moderation.mjs`, which is authoritative). It masks whole words after leetspeak folding and repeated-letter collapsing, and refuses links (`links_not_allowed`) and emails or phone numbers (`personal_info`). **The word list is a starter list.** The moderation owner should replace it with a vetted English, Filipino and French list, or a vendor.
- **Moderation queue**: `chat_reports` (schema 005 + **008**). Player reports are `source='player'`. Every masked or refused message is `source='filter'`, with a body snapshot, the player id and the reason.
  - `GET /v1/mod/queue?status=open|closed` and `POST /v1/mod/resolve {id, action: dismiss|delete_message|mute_24h|mute_7d, note?}` take `Authorization: Bearer $MODERATION_TOKEN`, which must be at least 24 characters. With no token set, both endpoints answer 401.
  - There is no reviewer UI. A mute sets `players.chat_muted_until`, and muted players get `403 chat_muted`.
- Retention of report rows: as counsel directs.

## Flipping back to Families (under 13)

1. **Google Play:** set the target audience to include under-13 ages and enroll in the Families program. Google reviews the whole app against the Families policy.
2. **Vendors:** every analytics, crash or ads SDK must be Families-capable (child-directed configuration, no AAID). Ads SDKs must be on Google's Families Self-Certified list. See `Docs/analytics-vendors.md`.
3. **Consent:** plug a verifiable parental-consent method into `KidSafety.Consent` (and the purchase parental gate) and confirm the `child` rules (`chat`, `iap`, `iap_max_per_day`). Until then children stay offline and can't buy.
4. **Config:** set `"audience_mode": "families"` in `child_safety.json` and `AUDIENCE_MODE=families` on the server, then ship a store update and redeploy the function.
5. **Existing accounts — age re-check on next launch:** in 13+ mode, an under-13 answer was blocked and its data deleted, and everyone else answered 13+. A Families build should re-ask anyone whose stored answer is missing or edited (the existing gate does this). Also decide with counsel whether 13+ accounts created under the teen policy need re-confirmation. That would mean bumping `AgeGateStore` `Version`, which forces a fresh neutral question for everyone.
6. **Data clean-up:** anything collected from 13+ players under teen-only vendors that a Families app can't keep must be removed or migrated.
7. **Legal and listing:** republish the privacy policy and Data safety for a child audience (`*.v2-families.md` drafts in Product's folder).

## Why the consent hook stays in a 13+ launch

Some soft-launch rules may still cover 13 to 17 year-old players:
- Australia's Children's Online Privacy Code defines a child as under 18.
- Quebec Law 25 requires parental consent for under-14s.

If counsel says consent is needed for some 13+ players, the hook (`IParentalConsentProvider`, `ConsentGate`) is still there to wire in.
