# Cloud saves (Workstream 3)

**Status: HOLD. Do not merge without Derek's OK.**

Stacked on kids C (#205 / `kids-consent-delete-20261109` tip `7a2f865e`), which sits on #204 ← #203 ← #201.

Local-first account saves on the Neon `alliance` Function (`GET`/`PUT /v1/save`). Offline play keeps working from PlayerPrefs / files; cloud sync runs on launch, resume, every ~120s, and after purchase — **only when** `KidSafety.AllowsCloudSave` is true (#205 `ConsentGate` + `IParentalConsentProvider`).

## What is saved
- Campaign, village (incl. producer stock)
- **Quests by chain id** (`quest.chain.starter` → `{index,progress}`) — works with the 3-step chain and the 32-step Theme A chain on PR #202
- FTUE step id
- **World gather remaining stock** (Quarry): `WorldMarchLoop.Available` was catalog-only and reset every launch; now `world.gather[nodeId].{available,updatedAt,regenAt?}`
- IAP applied hashes + grant ledger (refund clawback via RTDN)

## Kids / COPPA
- Blob has no personal info (server rejects email/name/ad id keys)
- Keyed only by `players.id`
- **Reuses #205** `KidSafety.AllowsCloudSave` / `ConsentPurpose.CloudSave` — no parallel consent type
- Under-13 default: local-only until a consent provider grants
- Account deletion (#205 `POST /v1/account/delete`) also deletes `player_saves` and `save_clawbacks`

## Neon
- Schema: `Backend/schema/007_cloud_saves.sql` (after kids 005/006)
- Applied for testing to branch `cloud-saves-dev-20261109` (`br-young-term-b8k2b6xd`), child of `iap-dev-20261109`
- **Not** applied to main
