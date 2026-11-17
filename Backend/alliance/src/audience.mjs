// Audience switch (mirrors the client's kids/child_safety.json audience_mode). Env AUDIENCE_MODE:
//  - "teen_plus" (default, 13+ soft launch): accounts with age_band 'under13' are blocked: no purchases, no cloud save, and
//    reporting under13 deletes the account's personal data (only the under13 marker is kept to enforce the block).
//  - "families" (Google Play Families, mixed audience): the previous behavior (under13 = restricted child mode, cloud
//    save behind parental consent, purchases gated on the client by the parental gate).
export const AUDIENCES = ["teen_plus", "families"];
export const TEEN_PLUS = "teen_plus";
export const FAMILIES = "families";

/** Unknown or missing values fall back to teen_plus (the launch audience). */
export function audienceMode(env = process.env) {
  const v = String(env?.AUDIENCE_MODE || "").trim().toLowerCase();
  return AUDIENCES.includes(v) ? v : TEEN_PLUS;
}

/** teen_plus: an under13 account may not use paid / data-collecting features at all. */
export const blockedByAudience = (audience, ageBand) => audience === TEEN_PLUS && ageBand === "under13";
