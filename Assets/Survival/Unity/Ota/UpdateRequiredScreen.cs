#nullable enable
using System;
using System.Collections.Concurrent;
using Survival.Domain.Theme;
using Survival.Unity.Kids;
using UnityEngine;

namespace Survival.Unity.Ota
{
    /// <summary>
    /// Blocks play when this build is below the remote config's min_supported_version. Android tries Play's immediate
    /// in-app update first; the button (and iOS) opens the store page, behind the parental gate in child mode (Families /
    /// Apple Kids: no link out of the app without a grown-up). Never dismissible: the old build cannot talk to the
    /// current content or servers.
    /// </summary>
    public sealed class UpdateRequiredScreen : MonoBehaviour
    {
        private static readonly ParentalGateScreen.Gate ParentGate = new ParentalGateScreen.Gate();
        private readonly ConcurrentQueue<Action> _main = new ConcurrentQueue<Action>();
        private string _storeUrl = "";
        private bool _triedPlay;

        public static void Show(string storeUrl)
        {
            var card = KidUi.Modal(null!, "UpdateRequiredCanvas", 900, 0.07f, 0.30f, 0.93f, 0.70f, out var root);
            var s = root.gameObject.AddComponent<UpdateRequiredScreen>();
            s._storeUrl = storeUrl;
            KidUi.Label(card, "Title", KidUi.S("ota.update.title", "Update needed"), 34, 0.05f, 0.72f, 0.95f, 0.92f);
            KidUi.Label(card, "Body", KidUi.S("ota.update.body", "A new version of Kingdom Defense is ready. Update to keep playing."), 22, 0.07f, 0.40f, 0.93f, 0.70f);
            KidUi.Button(card, "Update", KidUi.S("ota.update.button", "Update"), HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 26, 0.25f, 0.10f, 0.75f, 0.32f, s.OnUpdate);
            s.TryPlay();
        }

        private void Update()
        {
            while (_main.TryDequeue(out var a))
            {
                a();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            // Back from Play's update screen without updating (or it is still installing): offer it again.
            if (!paused)
            {
                TryPlay();
            }
        }

        private void TryPlay()
        {
            if (InAppUpdate.Platform != "android")
            {
                return;
            }

            InAppUpdate.TryStartImmediate(a => _main.Enqueue(a), shown => _triedPlay = !shown || _triedPlay);
        }

        private void OnUpdate()
        {
            if (InAppUpdate.Platform == "android" && !_triedPlay)
            {
                InAppUpdate.TryStartImmediate(a => _main.Enqueue(a), shown =>
                {
                    if (!shown)
                    {
                        _triedPlay = true;
                        OpenStore();
                    }
                });
                return;
            }

            OpenStore();
        }

        private void OpenStore()
        {
            if (string.IsNullOrEmpty(_storeUrl))
            {
                return;
            }

            if (!KidSafety.IsChild)
            {
                Application.OpenURL(_storeUrl);
                return;
            }

            ParentGate.Ask("update:store", ok =>
            {
                if (ok)
                {
                    Application.OpenURL(_storeUrl);
                }
            });
        }
    }
}
