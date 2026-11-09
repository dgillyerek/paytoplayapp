# Real purchases (Workstream 2): Unity IAP + Google Play validation

**Status: HOLD. Do not merge without Derek's OK.** Prices are placeholders. Free-player pacing is unchanged: the domain test results match `origin/main` byte for byte. Details are under *Tests* below.

## What this adds
- **Client (Unity)**
  - `com.unity.purchasing` **5.4.4**, which is Google Play Billing on Android. 5.5.0 shipped 2026-11-04, and I left it out on purpose.
  - The store sits behind a store-agnostic `IPlatformStore`. `UnityIapStore` uses the 5.x `StoreController` API, so StoreKit 2 works through the same adapter on iOS.
- **`PurchaseService`** (in Survival.Domain, so the net8 tests cover it):
  - Handles purchase, pending, deferred, restore and failure.
  - Only the server's response grants anything. The store purchase is confirmed (consumed or acknowledged) only after the server says it is `granted`.
  - Pending, rejected and unreachable purchases are never consumed. Unreachable ones are retried every 15 s and on the next launch, because the store redelivers them.
  - A local ledger (`iap.applied`, hashed keys) stops the same purchase being credited twice when the store redelivers it.
- **`IPurchaseGate`**: the age-band gate. It runs before the store sheet opens. See *Families / COPPA* below.
- **Build switch** (`SurvivalPurchases.Create`):
  - Live billing runs on release Android builds (`UNITY_ANDROID && !UNITY_EDITOR && !DEVELOPMENT_BUILD`), or anywhere `SURVIVAL_REAL_IAP` is defined.
  - The Editor and dev builds keep the existing sandbox. `SURVIVAL_SANDBOX_IAP` forces the sandbox.
  - If live billing fails to start, the store shows "unavailable". It **never** falls back to free sandbox grants.
- **Server** (`Backend/alliance`, the existing Neon Function, same account and bearer token):
  - `POST /v1/iap/validate`: Google Play `purchases.products.get`, recorded idempotently. It returns the grants for that product.
  - `GET /v1/iap/entitlements`, `GET /v1/account`, `POST /v1/account/age_band`.
  - `POST /v1/iap/rtdn?token=…`: Pub/Sub push handler for RTDN. It revokes on `voidedPurchaseNotification` and `oneTimeProductNotification` type 2 (cancelled). Each event is recorded once, keyed by `message_id`.
  - App Store Server API stub (`appstore.mjs`) behind the same verifier interface. It returns `501 app_store_not_enabled` until it is enabled.
- **Mapping**: `Assets/ThemePack/fantasy_kingdom_a/store/iap_rewards.json` maps each product to its reward. Grants mirror `stores.json`, and `merchant_bundle` mirrors `MerchantConfig`. The server bundles the same file. Domain tests fail if the file drifts from `products.json`, `stores.json` or `MerchantConfig`.
- **Schema**: `Backend/schema/004_purchases.sql`
  - `players.age_band` (`unknown` / `under13` / `13plus`, default `unknown`).
  - `iap_purchases` (UNIQUE `(store, purchase_token)`, which is the idempotency key).
  - `iap_grants` (UNIQUE `(purchase_id, player_id)`).
  - `player_entitlements`.
  - `iap_rtdn_events`.

## Endpoints (all JSON, bearer token from `/v1/signin` unless noted)
| Method | Path | Notes |
|---|---|---|
| POST | `/v1/iap/validate` | Body `{store:"google_play"\|"app_store", productId, purchaseToken, orderId?}`. Replies (all 200 unless a code is shown):<br>• `granted`, with `grants`, `entitlements`, `fresh`, `restored`, `purchaseId`<br>• `pending`<br>• `rejected`, with one of `invalid_token`, `cancelled`, `product_mismatch`, `already_consumed`, `refunded`, `voided`<br>• `400` for `bad_request` or `unknown_product`<br>• `409 owned_by_other_account`<br>• `501 app_store_not_enabled`<br>• `503 verify_unavailable` |
| GET | `/v1/iap/entitlements` | Active, non-revoked entitlements for this account. |
| GET | `/v1/account` | `{id, ageBand}`. |
| POST | `/v1/account/age_band` | `{ageBand}`, one of `under13`, `13plus`, `unknown`. |
| POST | `/v1/iap/rtdn?token=$IAP_RTDN_PUSH_TOKEN` | **No bearer.** Pub/Sub push. The token is compared in constant time. |

Live base URL (main branch function): `https://br-summer-firefly-b8or37b8-alliance.compute.c-14.us-east-1.aws.neon.tech`. **It does not have these routes until the function is redeployed and 004 is applied to main.**

## Neon
- Project: `paytoplay-survival` (`shiny-cherry-28583182`).
- Migration 004 was applied **only** to a new dev branch: `iap-dev-20261109` (`br-ancient-wildflower-b8h8c7k1`).
- **Not** applied to main (`br-summer-firefly-b8or37b8`). Apply 004 to main before you redeploy.

## Env vars (exact names)
| Name | Required | What |
|---|---|---|
| `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` | yes (Android) | The GCP service-account key JSON, raw or base64. Store it as a **secret**. It is never committed. If it is missing, validate returns `503 verify_unavailable`. |
| `GOOGLE_PLAY_PACKAGE_NAME` | optional | Defaults to `com.paytoplay.fantasykingdoma`, from `iap_rewards.json`. |
| `IAP_RTDN_PUSH_TOKEN` | yes (for RTDN) | A long random string. It goes in the Pub/Sub push URL. If it is unset, RTDN replies 401. |
| `DATABASE_URL` | existing | Unchanged. |
| `APP_STORE_ISSUER_ID`, `APP_STORE_KEY_ID`, `APP_STORE_PRIVATE_KEY`, `APP_STORE_BUNDLE_ID` | later (iOS) | Reserved. `appstore.mjs` is a stub. |
| `IAP_TEST_DATABASE_URL` | tests only | Points the Postgres integration test at a Neon **test** branch. |

## What Derek must provide
1. **Play Console in-app products** (one-time, "managed") with exactly these IDs:
   - `com.paytoplay.fantasykingdoma.` followed by `hard_s`, `hard_m`, `starter_pack`, `merchant_bundle`, `temp_builder`, `power_t1`…`power_t6`, `track_premium`, `track_royal`, `track_royal_upgrade` or `pass_season`.
   - Consumable vs non-consumable is decided in code (`iap_rewards.json`). In Play, all of them are one-time products.
2. **Final prices** for each product, per soft-launch country (CA, PH, AU, NZ). `products.json` is marked `"_prices": "PLACEHOLDER…"`. The UI shows the store's localized price once the product exists.
3. **GCP service account**
   - Link it in Play Console → Users & permissions, with **View financial data** and **Manage orders and subscriptions** for this app.
   - Enable the *Google Play Android Developer API* in its project.
   - Create a JSON key and set it as the secret `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` on the function.
4. **RTDN**
   - Create a Pub/Sub topic. Grant `google-play-developer-notifications@system.gserviceaccount.com` the Publisher role on it, and set it under Play Console → Monetization setup → Real-time developer notifications.
   - Add a **push** subscription to `https://<function-host>/v1/iap/rtdn?token=<IAP_RTDN_PUSH_TOKEN>`.
5. **Hosting**: I recommend keeping the existing Neon Function (`alliance`). Apply `004_purchases.sql` to main, set the env vars above, then redeploy (`npm run build`, then deploy as before).
6. **License key and Services link** in Unity: these are ProjectSettings changes, so I didn't make them (another workstream owns ProjectSettings and CI right now). Needed:
   - Unity Services cloud project link (Project Settings → Services). Without it, IAP only logs a warning; the app doesn't need it to work.
   - Google Play license key under Services → In-App Purchasing. It is optional, because the server does the validation.
   - Scripting define symbols, only if wanted: `SURVIVAL_SANDBOX_IAP` forces the sandbox on a release build, and `SURVIVAL_REAL_IAP` forces live billing in a dev or test build.
   - Nothing in `ProjectSettings/*.asset` or `.github/workflows` was edited.
7. **Product spec for Families** (see below): the parental-consent flow and the policy for unknown ages.
8. **Re-run the player bake**, so `iap_rewards.json` ships. `ThemePack/fantasy_kingdom_a` is already in the bake roots.

## Families / COPPA
The Theme A audience includes children under 13 (Google Play Families policy, COPPA). The soft launch is in CA, PH, AU and NZ.
- **No ad ID or device identifiers.**
  - The IAP path sends only the bearer token, product id, purchase token and order id. These are what Play Billing validation needs.
  - The account is the existing `players` row. It is keyed by an app-generated random GUID (sha256-hashed on the server), not by a hardware id.
  - `iap_purchases.store_payload` keeps only state fields (`orderId`, `purchaseState`, `consumptionState`, `acknowledgementState`, `purchaseTimeMillis`, `purchaseType`, `quantity`). The developer payload and obfuscated account ids are dropped.
- **No behavioral data.** There is no analytics or telemetry. The client never initializes `UnityServices`. **To do:** turn off IAP / Analytics "Developer Data" collection for the project in the Unity Dashboard before the build goes to the store.
- **`age_band`** on the account: `unknown` (default), `under13` or `13plus`. Set it with `POST /v1/account/age_band`. The client caches it in PlayerPrefs `account.age_band`.
- **`IPurchaseGate`** runs before every purchase:
  - `13plus` → allowed.
  - `under13` → `IParentalConsent`. The UI is a **stub** (`NoParentalConsent` always declines, failure `iap.consent_required`) until Product sends the requirements. Plug the real flow in with `SurvivalPurchases.ParentalConsent = …`.
  - `unknown` → **blocked by default** (`iap.age_unknown`). Product hasn't said that unknown may default to allow, so it doesn't. `SurvivalPurchases.AllowUnknownAge` is the switch.
  - **Result:** release builds can't make live purchases until an age screen sets the band. That is intentional.
- **Open privacy item (existing feature):** the alliance `display_name` is free text and could be PII for children. It is not part of this PR, and Product should review it.

## Accounts (shared with workstream 3, cloud saves)
- One account model: `players`.
- The id comes from a device-local random GUID. A reinstall makes a new account.
- Non-consumables restore to a new account as **entitlements only**, with no wallet re-grant.
- A consumable token already granted to another account → `409 owned_by_other_account`.
- Cloud saves should attach to `players.id`. Real account recovery (sign-in) is the workstream 3 decision.

## Follow-ups / known gaps
- Refunds revoke server entitlements and mark the purchase refunded or voided. Taking back wallet currency already spent needs server-side saves (workstream 3).
- Track or crusade unlocks bought while another screen is open (unsolicited, e.g. a redelivery) are recorded as server entitlements and restored from there. They are not yet pushed live into campaign state.
- iOS stays on the sandbox until the App Store Server API verifier is finished (`appstore.mjs` explains what's left).
- `packages-lock.json` isn't tracked, so Unity resolves `com.unity.purchasing` 5.4.4 and its dependencies (`services.core`) when it opens the project.

## Tests
- **Server** (`cd Backend/alliance && npm ci && npm test`): 44 passed, 1 skipped (the Postgres integration test, which needs `IAP_TEST_DATABASE_URL`).
  - Covers Google validation with Google mocked: JWT, error mapping, pending, cancelled, product mismatch, already consumed.
  - Covers idempotency: retries, concurrent duplicate validates, a purchase that goes pending then granted, and other-account refusal.
  - Covers grant mapping checked against `stores.json`, restore, RTDN void/cancel/idempotent/bad token, App Store 501, and minimal payload.
- **Postgres integration** (`test/iap_pg.test.mjs`): passed against Neon dev branch `iap-dev-20261109`. The tables were empty again afterward.
- **Bundle**: `npm run build` succeeds (`dist/index.mjs`, 244 KB), and a smoke test passed (`/v1/health` 200, `/v1/iap/rtdn` with a bad token 401).
- **Domain** (`dotnet test tests/Survival.Domain.Tests`): 41 new IAP tests, all passing.
  - The full suite was run on this branch and on `origin/main` in the same sparse checkout (without `design/` and `ArtSource/`).
  - Both runs had the identical 71 failures, with identical messages. Most need `design/`. That includes `RunnerFreePaceTests`, which also fails on main in this checkout.
  - No new failures, and the free-pace numbers are unchanged.
