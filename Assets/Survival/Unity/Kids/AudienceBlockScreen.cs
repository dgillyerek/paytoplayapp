#nullable enable
using System;
using Survival.Domain.Kids;
using Survival.Domain.Theme;
using UnityEngine;

namespace Survival.Unity.Kids
{
    /// <summary>
    /// 13+ audience (child_safety.json audience_mode teen_plus): shown instead of the game when the stored age answer is
    /// under 13 (or was edited). Polite, no buttons, no way back to the age question on this install. On show it deletes
    /// whatever was collected (AudienceBlock: the server account first if one exists, then the device) and writes back only
    /// the under-13 record so the block holds. If the server cannot be reached nothing is wiped and the next launch retries.
    /// </summary>
    public sealed class AudienceBlockScreen : MonoBehaviour
    {
        private RectTransform _card = null!;
        private AgeBandDef? _kept;

        /// <summary>Covers everything (the game never continues past this screen).</summary>
        public static void Show(Transform? parent)
        {
            var card = KidUi.Modal(parent!, "AudienceBlockCanvas", 600, 0.05f, 0.20f, 0.95f, 0.80f, out var root);
            var screen = root.gameObject.AddComponent<AudienceBlockScreen>();
            screen._card = card;
            screen.Build();
            screen.Enforce();
        }

        private void Build()
        {
            HookSkin.Header(_card, KidUi.S("kids.block.title", "Sorry!"), 520f, 96f, 34);
            KidUi.Label(_card, "Body", KidUi.S("kids.block.body", "Kingdom Defense is made for players aged 13 and over, so it isn't available to you. Thanks for stopping by!"), 28, 0.07f, 0.40f, 0.93f, 0.84f);
            var email = KidSafety.Config.SupportEmail;
            if (!string.IsNullOrWhiteSpace(email) && !email.StartsWith("[", StringComparison.Ordinal))
            {
                KidUi.Label(_card, "Parent", KidUi.S("kids.block.parent", "Parents: questions about your child's data? Email") + " " + email, 18, 0.07f, 0.10f, 0.93f, 0.30f, SurvivalVisuals.Mute);
            }
        }

        private void Enforce()
        {
            var policy = KidSafety.Policy;
            var safety = AllianceClient.Safety;
            var deletion = new AccountDeletion(
                () => PlayerPrefs.HasKey(SurvivalCampaignView.DeviceIdPref),
                done => DeleteOnServer(safety, done),
                SettingsScreen.WipeDevice);
            AudienceBlock.Enforce(policy, deletion, Keep, r => Debug.Log("[Kids] 13+ audience block: " + r));
        }

        /// <summary>Only what enforces the block survives: the signed under-13 age record.</summary>
        private void Keep(AgeBandDef band)
        {
            _kept = band;
            KidSafety.Store.MarkShown();
            KidSafety.Store.Save(band);
        }

        // Anything loaded before the block (a session autosave on pause / quit) must not write data back: wipe again.
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Rewipe();
            }
        }

        private void OnApplicationQuit() => Rewipe();

        private void Rewipe()
        {
            if (_kept == null)
            {
                return;
            }

            SettingsScreen.WipeDevice();
            Keep(_kept);
        }

        /// <summary>Signed in now: delete with that session. Otherwise sign in with the stored device id just to delete it.</summary>
        private void DeleteOnServer(AllianceSafetyClient? safety, Action<bool> done)
        {
            if (safety != null && !string.IsNullOrEmpty(AllianceClient.SessionToken))
            {
                safety.DeleteAccount(done);
                return;
            }

            var api = AllianceClient.ApiUrl;
            var device = PlayerPrefs.GetString(SurvivalCampaignView.DeviceIdPref, "");
            if (api.Length == 0 || device.Length < 8)
            {
                // No server configured, or no usable device id: nothing server-side can belong to this install.
                done(api.Length == 0);
                return;
            }

            var net = AllianceClient.Connect(this, api);
            net.SignIn(device, ChatSafety.SafeName(device), ok =>
            {
                if (!ok || AllianceClient.Safety == null)
                {
                    done(false);
                    return;
                }

                AllianceClient.Safety.DeleteAccount(done);
            });
        }
    }
}
