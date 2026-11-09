#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Alliance;
using Survival.Domain.Serialization;

namespace Survival.Domain.Iap
{
    /// <summary>
    /// Sends a store transaction to the backend (POST /v1/iap/validate on Backend/alliance) and maps its reply.
    /// Uses the same player account and bearer token as the alliance client (one account model for IAP, alliance and,
    /// next, cloud saves). The reply's grants are the only grants the client ever credits for a real purchase.
    /// </summary>
    public sealed class ServerReceiptVerifier : IReceiptVerifier
    {
        public const string ValidatePath = "/v1/iap/validate";

        private readonly IAllianceTransport _http;
        private readonly Func<string?> _token;

        public ServerReceiptVerifier(IAllianceTransport http, Func<string?> token)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public static string RequestBody(StoreTransaction tx) =>
            "{\"store\":" + AllianceService.Q(tx.Store == IapStore.GooglePlay ? "google_play" : "app_store")
            + ",\"productId\":" + AllianceService.Q(tx.ProductId)
            + ",\"purchaseToken\":" + AllianceService.Q(tx.PurchaseToken)
            + ",\"transactionId\":" + AllianceService.Q(tx.TransactionId)
            + (tx.OrderId == null ? "" : ",\"orderId\":" + AllianceService.Q(tx.OrderId))
            + (tx.SignedTransaction == null ? "" : ",\"signedTransaction\":" + AllianceService.Q(tx.SignedTransaction))
            + "}";

        public void Verify(StoreTransaction tx, Action<VerifyResult> done)
        {
            var token = _token();
            if (string.IsNullOrEmpty(token))
            {
                done(VerifyResult.Fail(VerifyStatus.Unavailable, "iap.not_signed_in"));
                return;
            }

            _http.Send("POST", ValidatePath, RequestBody(tx), token, (status, text) => done(Map(status, text)));
        }

        /// <summary>HTTP status + body → result. 200 {status: granted|pending|rejected}; 4xx = rejected; 0 / 401 / 429 / 5xx = retry later.</summary>
        public static VerifyResult Map(int status, string text)
        {
            JsonNode.Obj? o = null;
            try
            {
                o = JsonRead.Parse(string.IsNullOrEmpty(text) ? "{}" : text) as JsonNode.Obj;
            }
            catch (FormatException)
            {
            }

            var error = o != null && o.TryGet("error", out var e) && e is JsonNode.Str es ? "iap." + es.Value : null;
            if (status == 0 || status == 401 || status == 408 || status == 429 || status >= 500 || o == null)
            {
                return VerifyResult.Fail(VerifyStatus.Unavailable, error ?? (status == 0 ? "iap.offline" : "iap.http_" + status));
            }

            if (status < 200 || status >= 300)
            {
                return VerifyResult.Fail(VerifyStatus.Rejected, error ?? "iap.rejected");
            }

            var state = o.TryGet("status", out var s) && s is JsonNode.Str ss ? ss.Value : "";
            switch (state)
            {
                case "granted":
                    var grants = new Dictionary<string, int>(StringComparer.Ordinal);
                    if (o.TryGet("grants", out var g) && g is JsonNode.Obj go)
                    {
                        foreach (var kv in go.Fields)
                        {
                            if (kv.Value is JsonNode.Num n && n.Value > 0)
                            {
                                grants[kv.Key] = n.AsInt();
                            }
                        }
                    }

                    var ents = new List<string>();
                    if (o.TryGet("entitlements", out var en) && en is JsonNode.Arr ea)
                    {
                        foreach (var item in ea.Items)
                        {
                            if (item is JsonNode.Str str)
                            {
                                ents.Add(str.Value);
                            }
                        }
                    }

                    return new VerifyResult(VerifyStatus.Granted, grants, ents);
                case "pending":
                    return VerifyResult.Fail(VerifyStatus.Pending, "iap.pending");
                default:
                    var reason = o.TryGet("reason", out var r) && r is JsonNode.Str rs ? "iap." + rs.Value : "iap.rejected";
                    return VerifyResult.Fail(VerifyStatus.Rejected, reason);
            }
        }
    }
}
