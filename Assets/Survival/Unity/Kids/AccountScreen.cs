#nullable enable
using System;
using System.IO;
using Survival.Domain.Kids;
using Survival.Domain.Theme;
using UnityEngine;

namespace Survival.Unity.Kids
{
    /// <summary>
    /// "Privacy and account": in-app account deletion (Play account-deletion policy, COPPA 312.6 parent deletion right)
    /// plus the web deletion page from child_safety.json account.web_deletion_url. In child mode (and unknown age) the
    /// parental gate comes first and the web link is gated too (Families: no unguarded links out of the app). Adults get
    /// a plain confirm. Opened from the player name on the HUD.
    /// </summary>
    public sealed class AccountScreen : MonoBehaviour
    {
        private static readonly ParentalGateScreen.Gate ParentGate = new ParentalGateScreen.Gate();

        public static void Show()
        {
            var card = KidUi.Modal(null!, "AccountCanvas", 500, 0.07f, 0.24f, 0.93f, 0.76f, out var root);
            root.gameObject.AddComponent<AccountScreen>().Menu(card);
        }

        private RectTransform _card = null!;

        private void Clear()
        {
            for (var i = _card.childCount - 1; i >= 0; i--)
            {
                Destroy(_card.GetChild(i).gameObject);
            }
        }

        private void Close() => Destroy(gameObject);

        private void Menu(RectTransform card)
        {
            _card = card;
            Clear();
            HookSkin.Header(card, KidUi.S("kids.account.title", "Privacy and account"), 520f, 96f, 32);
            var url = KidSafety.Config.WebDeletionUrl;
            KidUi.Label(card, "Info", KidUi.S("kids.account.info", "You can delete your account and all game data. This can't be undone."), 22, 0.06f, 0.66f, 0.94f, 0.84f);
            KidUi.Button(card, "Delete", KidUi.S("kids.account.delete", "Delete account and data"), HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 24, 0.12f, 0.46f, 0.88f, 0.58f, () => Guarded("delete", Confirm));
            if (!string.IsNullOrWhiteSpace(url))
            {
                KidUi.Button(card, "Web", KidUi.S("kids.account.web", "Delete on the web instead"), HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 22, 0.12f, 0.31f, 0.88f, 0.42f, () => Guarded("link", () => Application.OpenURL(url)));
                KidUi.Label(card, "Url", url, 16, 0.06f, 0.21f, 0.94f, 0.29f, SurvivalVisuals.Mute);
            }

            KidUi.Button(card, "Close", KidUi.S("kids.account.close", "Close"), HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 24, 0.30f, 0.04f, 0.70f, 0.15f, Close);
        }

        /// <summary>Child / unknown age: a grown-up answers the gate first. 13+: straight through.</summary>
        private static void Guarded(string purpose, Action then)
        {
            if (!KidSafety.IsChild)
            {
                then();
                return;
            }

            ParentGate.Ask("account:" + purpose, ok =>
            {
                if (ok)
                {
                    then();
                }
            });
        }

        private void Confirm()
        {
            Clear();
            HookSkin.Header(_card, KidUi.S("kids.account.confirm_title", "Delete everything?"), 520f, 96f, 32);
            KidUi.Label(_card, "Sure", KidUi.S("kids.account.confirm", "Your progress, guild membership and messages will be deleted from this device and our server. Purchases can't be restored after this."), 22, 0.06f, 0.42f, 0.94f, 0.84f);
            KidUi.Button(_card, "Back", KidUi.S("kids.account.back", "Keep my account"), HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 22, 0.06f, 0.06f, 0.48f, 0.18f, () => Menu(_card));
            KidUi.Button(_card, "Yes", KidUi.S("kids.account.yes", "Delete"), HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 22, 0.52f, 0.06f, 0.94f, 0.18f, Run);
        }

        private void Run()
        {
            Clear();
            KidUi.Label(_card, "Working", KidUi.S("kids.account.working", "Deleting\u2026"), 26, 0.06f, 0.40f, 0.94f, 0.60f);
            var safety = SurvivalCampaignView.AllianceSafety;
            // Ever signed in (a device id exists) means there is a server account: it must be deleted first. Not signed in
            // right now (offline) is a server failure, so nothing local is wiped and the account is never orphaned.
            new AccountDeletion(
                () => PlayerPrefs.HasKey(SurvivalCampaignView.DeviceIdPref),
                done =>
                {
                    if (safety == null || string.IsNullOrEmpty(SurvivalCampaignView.AllianceNet?.SessionToken))
                    {
                        done(false);
                        return;
                    }

                    safety.DeleteAccount(done);
                },
                WipeDevice).Run(Done);
        }

        private void Done(DeletionResult result)
        {
            Clear();
            if (result == DeletionResult.ServerFailed)
            {
                KidUi.Label(_card, "Failed", KidUi.S("kids.account.failed", "We couldn't reach the server, so nothing was deleted. Check the connection and try again, or use the web page."), 22, 0.06f, 0.36f, 0.94f, 0.84f);
                KidUi.Button(_card, "Back", KidUi.S("kids.account.back_menu", "Back"), HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 24, 0.30f, 0.06f, 0.70f, 0.18f, () => Menu(_card));
                return;
            }

            KidUi.Label(_card, "Gone", KidUi.S("kids.account.done", "Your account and data are deleted. The game will now close."), 24, 0.06f, 0.36f, 0.94f, 0.84f);
            KidUi.Button(_card, "Ok", KidUi.S("kids.account.ok", "OK"), HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 24, 0.30f, 0.06f, 0.70f, 0.18f, () =>
            {
                // Wipe again in case an autosave ran meanwhile, then quit so no in-memory state is written back.
                WipeDevice();
                Application.Quit();
            });
        }

        /// <summary>Everything this app stores on the device: PlayerPrefs (age record, device id, token, settings) and saves.</summary>
        public static void WipeDevice()
        {
            KidSafety.ClearForAccountDeletion();
            SurvivalCampaignView.ForgetAllianceNet();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            foreach (var dir in new[] { Application.persistentDataPath, Application.temporaryCachePath })
            {
                try
                {
                    if (!Directory.Exists(dir))
                    {
                        continue;
                    }

                    foreach (var f in Directory.GetFiles(dir))
                    {
                        File.Delete(f);
                    }

                    foreach (var d in Directory.GetDirectories(dir))
                    {
                        Directory.Delete(d, true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
