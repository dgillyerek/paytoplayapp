#nullable enable
using System;
using System.IO;
using Survival.Domain.Kids;
using Survival.Domain.Localization;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Survival.Unity.Kids
{
    /// <summary>
    /// Settings, opened from the HUD gear. One section for now, "Privacy and account":
    /// <list type="bullet">
    /// <item>age group (read-only; set once by the age gate, reset only by deleting the account),</item>
    /// <item>guild chat (off / ready-made messages / free; children never see free, and turning chat on needs a grown-up),</item>
    /// <item>Families audience: parental consent status for online play (and "Ask a grown-up" once a consent method is plugged
    /// in); 13+ audience: "Update my age" instead (lower only; under 13 blocks play and deletes the data),</item>
    /// <item>privacy policy and terms links (child_safety.json legal.*; behind the parental gate in child mode),</item>
    /// <item>delete account (Play account-deletion policy, COPPA 312.6): parental gate in child mode, a confirm step for
    /// 13+, the server first and then the device; plus the web deletion page as a fallback.</item>
    /// </list>
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        private static readonly ParentalGateScreen.Gate ParentGate = new ParentalGateScreen.Gate();

        public static void Show()
        {
            var card = KidUi.Modal(null!, "SettingsCanvas", 500, 0.05f, 0.10f, 0.95f, 0.90f, out var root);
            root.gameObject.AddComponent<SettingsScreen>().Menu(card);
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

        private static string ChatLabel(ChatMode m) => m switch
        {
            ChatMode.Free => KidUi.S("kids.settings.chat.free", "Free chat"),
            ChatMode.Presets => KidUi.S("kids.settings.chat.presets", "Ready-made messages"),
            _ => KidUi.S("kids.settings.chat.off", "Off"),
        };

        private static string AgeLabel(string band) => band switch
        {
            "under13" => KidUi.S("kids.settings.age.under13", "Under 13"),
            "13plus" => KidUi.S("kids.settings.age.13plus", "13 and over"),
            _ => KidUi.S("kids.settings.age.unknown", "Not set"),
        };

        private static string ConsentLabel(ConsentView c) => c switch
        {
            ConsentView.NotNeeded => KidUi.S("kids.settings.consent.not_needed", "Not needed"),
            ConsentView.Granted => KidUi.S("kids.settings.consent.granted", "Given by a grown-up. Online play is on."),
            ConsentView.Pending => KidUi.S("kids.settings.consent.pending", "Waiting for a grown-up. Online play is off for now."),
            ConsentView.Denied => KidUi.S("kids.settings.consent.denied", "Not given. Online play stays off."),
            ConsentView.Unavailable => KidUi.S("kids.settings.consent.unavailable", "Online play is off for players under 13."),
            _ => KidUi.S("kids.settings.consent.needed", "Needed before online play. The guild plays offline until then."),
        };

        private void Menu(RectTransform card)
        {
            _card = card;
            Clear();
            var s = new PrivacySettings(KidSafety.Policy, KidSafety.Consent);
            var c = KidSafety.Config;
            HookSkin.Header(card, KidUi.S("kids.settings.title", "Settings"), 520f, 96f, 32);
            KidUi.Label(card, "Section", KidUi.S("kids.settings.privacy", "Privacy and account"), 26, 0.05f, 0.83f, 0.55f, 0.875f, SurvivalVisuals.Reward);
            var lang = Locales.Find(SurvivalLocale.Current)?.NativeName ?? "English";
            KidUi.Button(card, "Language", lang + "  ▸", HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 20, 0.57f, 0.83f, 0.95f, 0.88f, () => LanguageMenu());

            KidUi.Label(card, "Age", KidUi.S("kids.settings.age", "Age group") + ": " + AgeLabel(s.AgeBandKey), 22, 0.05f, 0.775f, 0.95f, 0.82f, null, TextAnchor.MiddleLeft);
            KidUi.Label(card, "AgeNote", (s.CanCorrectAge ? KidUi.S("kids.settings.age_note_teen", "Set when you first played.") : KidUi.S("kids.settings.age_note", "Set when you first played. Deleting your account resets it.")), 16, 0.05f, 0.745f, 0.95f, 0.775f, SurvivalVisuals.Mute, TextAnchor.MiddleLeft);

            KidUi.Label(card, "Chat", KidUi.S("kids.settings.chat", "Guild chat") + ": " + ChatLabel(s.Chat), 22, 0.05f, 0.69f, 0.95f, 0.735f, null, TextAnchor.MiddleLeft);
            var n = s.ChatChoices.Count;
            for (var i = 0; i < n; i++)
            {
                var mode = s.ChatChoices[i];
                var x0 = 0.05f + i * (0.90f / n);
                var current = mode == s.Chat;
                KidUi.Button(card, "Chat" + mode, ChatLabel(mode), current ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonBlue, current ? SurvivalVisuals.MineFill : SurvivalVisuals.PinBuild, 20, x0 + 0.01f, 0.615f, x0 + 0.90f / n - 0.01f, 0.685f, () => ChooseChat(s, mode));
            }

            if (s.Policy.IsChild)
            {
                KidUi.Label(card, "ChatNote", KidUi.S("kids.settings.chat_note", "Under 13: no typing, only ready-made messages. A grown-up turns chat on."), 16, 0.05f, 0.58f, 0.95f, 0.612f, SurvivalVisuals.Mute, TextAnchor.MiddleLeft);
            }

            if (s.CanCorrectAge)
            {
                // 13+ audience: the parental-consent row is hidden (the hook stays in code). Age correction: lower only.
                KidUi.Button(card, "AgeFix", KidUi.S("kids.settings.age_fix", "Update my age"), HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 20, 0.25f, 0.47f, 0.75f, 0.545f, CorrectAge);
            }

            if (s.ShowConsent)
            {
                KidUi.Label(card, "Consent", KidUi.S("kids.settings.consent", "Parental consent") + ": " + ConsentLabel(s.Consent), 20, 0.05f, 0.50f, 0.95f, 0.575f, null, TextAnchor.MiddleLeft);
            }

            if (s.ShowConsent && s.CanRequestConsent)
            {
                KidUi.Button(card, "AskConsent", KidUi.S("kids.settings.consent_ask", "Ask a grown-up"), HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 20, 0.25f, 0.44f, 0.75f, 0.495f,
                    () => Guarded(s.Policy.IsChild, "consent", () => KidSafety.Consent.Request(ConsentPurpose.OnlineSignIn, _ => Menu(_card))));
            }

            Link(card, "Privacy", KidUi.S("kids.settings.privacy_policy", "Privacy policy"), c.PrivacyPolicyUrl, s.LinkNeedsParent, 0.05f, 0.48f);
            Link(card, "Terms", KidUi.S("kids.settings.terms", "Terms of service"), c.TermsUrl, s.LinkNeedsParent, 0.52f, 0.95f);

            KidUi.Button(card, "Delete", KidUi.S("kids.account.delete", "Delete account and data"), HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 22, 0.10f, 0.22f, 0.90f, 0.30f, () => Guarded(s.DeleteNeedsParent, "delete", Confirm));
            var url = c.WebDeletionUrl;
            if (!string.IsNullOrWhiteSpace(url))
            {
                KidUi.Button(card, "Web", KidUi.S("kids.account.web", "Delete on the web instead"), HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 18, 0.20f, 0.145f, 0.80f, 0.205f, () => Guarded(s.LinkNeedsParent, "link", () => Application.OpenURL(url)));
            }

            KidUi.Button(card, "Close", KidUi.S("kids.account.close", "Close"), HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 24, 0.30f, 0.03f, 0.70f, 0.11f, Close);
        }

        /// <summary>Language list: "device language" plus every shipped locale, each in its own language.</summary>
        private void LanguageMenu()
        {
            Clear();
            HookSkin.Header(_card, KidUi.S("settings.language.title", "Language"), 520f, 96f, 32);
            var saved = SurvivalLocale.Saved;
            var device = Locales.Find(Locales.Resolve(null, Application.systemLanguage.ToString()))?.NativeName ?? "English";
            var followDevice = string.IsNullOrEmpty(saved);
            KidUi.Button(_card, "LangDevice", KidUi.S("settings.language.device", "Device language") + " (" + device + ")",
                followDevice ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonBlue, followDevice ? SurvivalVisuals.MineFill : SurvivalVisuals.PinBuild,
                20, 0.10f, 0.80f, 0.90f, 0.86f, () => Switch(""));

            const float top = 0.78f, rowH = 0.066f;
            for (var i = 0; i < Locales.All.Count; i++)
            {
                var l = Locales.All[i];
                var y1 = top - (i / 2) * rowH;
                var x0 = i % 2 == 0 ? 0.05f : 0.51f;
                var on = !followDevice && string.Equals(saved, l.Code, StringComparison.OrdinalIgnoreCase);
                var code = l.Code;
                KidUi.Button(_card, "Lang_" + code, l.NativeName, on ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonBlue, on ? SurvivalVisuals.MineFill : SurvivalVisuals.PinBuild,
                    18, x0, y1 - rowH + 0.008f, x0 + 0.44f, y1, () => Switch(code));
            }

            KidUi.Button(_card, "Back", KidUi.S("kids.account.back_menu", "Back"), HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 24, 0.30f, 0.03f, 0.70f, 0.11f, () => Menu(_card));
        }

        /// <summary>Saves the choice and reloads the scene so every screen rebuilds in the new language.</summary>
        private void Switch(string code)
        {
            var before = SurvivalLocale.Current;
            SurvivalLocale.Choose(code);
            if (SurvivalLocale.Current == before)
            {
                Menu(_card);
                return;
            }

            var campaign = FindAnyObjectByType<SurvivalCampaignView>();
            if (campaign != null)
            {
                campaign.SaveNow();
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void CorrectAge() => AgeGateScreen.ShowCorrection(blocked =>
        {
            if (!blocked)
            {
                return;
            }

            // Now under 13 in a 13+ game: close Settings and block play; the block screen deletes the collected data.
            Close();
            AudienceBlockScreen.Show(null);
        });

        private static void Link(RectTransform card, string name, string label, string url, bool needsParent, float x0, float x1)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            KidUi.Button(card, name, label, HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 18, x0, 0.33f, x1, 0.40f, () => Guarded(needsParent, "link", () => Application.OpenURL(url)));
        }

        private void ChooseChat(PrivacySettings s, ChatMode to)
        {
            if (to == s.Chat || !s.CanChoose(to))
            {
                return;
            }

            Guarded(s.ChangeNeedsParent(to), "chat", () =>
            {
                KidSafety.SetChat(to);
                // Mirror to the account; the server clamps again (a child is never free).
                AllianceClient.Safety?.ApplyPolicy(KidSafety.Policy, ChatSafety.SafeName(PlayerPrefs.GetString(SurvivalCampaignView.DeviceIdPref, "")));
                if (_card != null)
                {
                    Menu(_card);
                }
            });
        }

        /// <summary>Child / unknown age: a grown-up answers the gate first. 13+: straight through.</summary>
        private static void Guarded(bool needsParent, string purpose, Action then)
        {
            if (!needsParent)
            {
                then();
                return;
            }

            ParentGate.Ask("settings:" + purpose, ok =>
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
            var safety = AllianceClient.Safety;
            // Ever signed in (a device id exists) means there is a server account: it must be deleted first. Not signed in
            // right now (offline) is a server failure, so nothing local is wiped and the account is never orphaned.
            new AccountDeletion(
                () => PlayerPrefs.HasKey(SurvivalCampaignView.DeviceIdPref),
                done =>
                {
                    if (safety == null || string.IsNullOrEmpty(AllianceClient.SessionToken))
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
            AllianceClient.Forget();
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
