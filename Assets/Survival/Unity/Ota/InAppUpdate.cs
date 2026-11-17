#nullable enable
using System;
using UnityEngine;

namespace Survival.Unity.Ota
{
    /// <summary>
    /// Store update for a build below min_supported_version.
    /// Android: Google Play In-App Updates, immediate flow (Play's own full-screen update UI; the game cannot be used until
    /// it finishes). A thin JNI wrapper over com.google.android.play:app-update, which the Editor hook
    /// Survival.Editor.Ota.AndroidInAppUpdateGradle adds to the generated Gradle project, so no Unity package is needed.
    /// The library adds no permissions and no advertising ID (the CI AD_ID check still runs on every .aab).
    /// iOS (and Android when Play cannot run the flow, e.g. a sideloaded phone build): the store page link.
    /// </summary>
    public static class InAppUpdate
    {
        public const string AppUpdateDependency = "com.google.android.play:app-update:2.1.0";

        private const int AppUpdateTypeImmediate = 1;
        private const int UpdateAvailable = 2;
        private const int DeveloperTriggeredUpdateInProgress = 3;

        /// <summary>
        /// Starts Play's immediate update flow. <paramref name="done"/> (main thread) gets true when the flow was shown,
        /// false when Play could not offer it (then show the store link instead).
        /// </summary>
        public static void TryStartImmediate(Action<Action> toMainThread, Action<bool> done)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                var factory = new AndroidJavaClass("com.google.android.play.core.appupdate.AppUpdateManagerFactory");
                var manager = factory.CallStatic<AndroidJavaObject>("create", activity);
                var infoTask = manager.Call<AndroidJavaObject>("getAppUpdateInfo");
                infoTask.Call<AndroidJavaObject>("addOnSuccessListener", new SuccessListener(info =>
                {
                    try
                    {
                        var availability = info.Call<int>("updateAvailability");
                        var allowed = info.Call<bool>("isUpdateTypeAllowed", AppUpdateTypeImmediate);
                        if ((availability == UpdateAvailable && allowed) || availability == DeveloperTriggeredUpdateInProgress)
                        {
                            var options = new AndroidJavaClass("com.google.android.play.core.appupdate.AppUpdateOptions")
                                .CallStatic<AndroidJavaObject>("defaultOptions", AppUpdateTypeImmediate);
                            manager.Call<AndroidJavaObject>("startUpdateFlow", info, activity, options);
                            toMainThread(() => done(true));
                            return;
                        }

                        Debug.LogWarning("OTA: Play offers no immediate update (availability " + availability + ", allowed " + allowed + ").");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("OTA: Play in-app update failed: " + ex.Message);
                    }

                    toMainThread(() => done(false));
                }));
                infoTask.Call<AndroidJavaObject>("addOnFailureListener", new FailureListener(() => toMainThread(() => done(false))));
            }
            catch (Exception ex)
            {
                // No Play Store, or the library is missing from the build.
                Debug.LogWarning("OTA: Play in-app updates unavailable: " + ex.Message);
                done(false);
            }
#else
            done(false);
#endif
        }

        /// <summary>Platform key used in the remote config ("android", "ios"; "" elsewhere = never blocked).</summary>
        public static string Platform
        {
            get
            {
#if UNITY_ANDROID
                return "android";
#elif UNITY_IOS
                return "ios";
#else
                return "";
#endif
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private sealed class SuccessListener : AndroidJavaProxy
        {
            private readonly Action<AndroidJavaObject> _onSuccess;

            public SuccessListener(Action<AndroidJavaObject> onSuccess) : base("com.google.android.gms.tasks.OnSuccessListener") => _onSuccess = onSuccess;

            // Java: void onSuccess(TResult result)
            public void onSuccess(AndroidJavaObject result) => _onSuccess(result);
        }

        private sealed class FailureListener : AndroidJavaProxy
        {
            private readonly Action _onFailure;

            public FailureListener(Action onFailure) : base("com.google.android.gms.tasks.OnFailureListener") => _onFailure = onFailure;

            // Java: void onFailure(Exception e)
            public void onFailure(AndroidJavaObject e)
            {
                Debug.LogWarning("OTA: Play in-app update info failed: " + (e?.Call<string>("getMessage") ?? "unknown"));
                _onFailure();
            }
        }
#endif
    }
}
