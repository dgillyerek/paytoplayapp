#nullable enable
using Survival.Domain.Alliance;
using Survival.Domain.Kids;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// The one signed-in alliance account for the app: the API URL, the alliance service and the safety client on the
    /// same sign-in. The campaign view signs in; purchases, settings, the guild and the world map read it from here.
    /// </summary>
    public static class AllianceClient
    {
        /// <summary>Public URL of the alliance Neon Function. Not a secret; override with PlayerPrefs "alliance.api" ("" = offline).</summary>
        public const string DefaultApi = "https://br-summer-firefly-b8or37b8-alliance.compute.c-14.us-east-1.aws.neon.tech";

        public const string ApiPref = "alliance.api";

        /// <summary>The configured API URL, or "" when the alliance is switched off.</summary>
        public static string ApiUrl
        {
            get
            {
                var api = PlayerPrefs.GetString(ApiPref, DefaultApi);
                return string.IsNullOrWhiteSpace(api) ? "" : api;
            }
        }

        /// <summary>Signed-in alliance service (null until the campaign view connects, or when the API is switched off).</summary>
        public static AllianceService? Net { get; private set; }

        /// <summary>Report / block and the server chat mode on the same sign-in (Kids, COPPA / Families).</summary>
        public static AllianceSafetyClient? Safety { get; private set; }

        /// <summary>Bearer token of the signed-in account, or null.</summary>
        public static string? SessionToken => Net?.SessionToken;

        /// <summary>Creates the shared service on <paramref name="runner"/>'s coroutines. The caller signs it in.</summary>
        public static AllianceService Connect(MonoBehaviour runner, string api)
        {
            Net = new AllianceService(new UnityAllianceTransport(runner, api));
            Safety = new AllianceSafetyClient(Net);
            return Net;
        }

        /// <summary>Account deletion (and every fresh Play session): drop the signed-in session so nothing more is sent.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Forget()
        {
            Net = null;
            Safety = null;
        }
    }
}
