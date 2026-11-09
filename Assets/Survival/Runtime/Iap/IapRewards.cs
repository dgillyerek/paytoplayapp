#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Serialization;

namespace Survival.Domain.Iap
{
    public enum IapProductType
    {
        /// <summary>Consumed once the server has confirmed it; can be bought again.</summary>
        Consumable,

        /// <summary>Acknowledged once; the server keeps the entitlement per account and restores it.</summary>
        NonConsumable
    }

    /// <summary>One theme-pack sku as the store needs it: its store product id and how the store treats it.</summary>
    public sealed record IapProduct(string Sku, string ProductId, IapProductType Type);

    /// <summary>What the server grants for one sku (iap_rewards.json). The client reads only Type; grants come from the server's reply.</summary>
    public sealed record IapReward(
        string Sku,
        IapProductType Type,
        IReadOnlyDictionary<string, int> Grants,
        IReadOnlyList<string> Entitlements);

    /// <summary>
    /// Theme pack store/iap_rewards.json: the sku → reward table the backend grants from. The client uses it only to
    /// tell the store which products are consumable. It never grants from it.
    /// </summary>
    public sealed class IapRewardCatalog
    {
        public const string FileName = "iap_rewards.json";
        public const string Consumable = "consumable";
        public const string NonConsumable = "non_consumable";

        private readonly Dictionary<string, IapReward> _bySku;

        private IapRewardCatalog(string packageName, Dictionary<string, IapReward> bySku)
        {
            PackageName = packageName;
            _bySku = bySku;
        }

        public string PackageName { get; }
        public IEnumerable<IapReward> Rewards => _bySku.Values;

        public IapReward? Find(string sku) => _bySku.TryGetValue(sku, out var r) ? r : null;

        public static IapRewardCatalog Parse(string json)
        {
            var root = LayoutJson.Root(json, FileName);
            var map = new Dictionary<string, IapReward>(StringComparer.Ordinal);
            foreach (var o in LayoutJson.Objs(root, "rewards"))
            {
                var sku = LayoutJson.Str(o, "sku");
                var typeName = LayoutJson.Str(o, "type");
                var type = typeName switch
                {
                    Consumable => IapProductType.Consumable,
                    NonConsumable => IapProductType.NonConsumable,
                    _ => throw new FormatException(FileName + ": " + sku + " has unknown type '" + typeName + "'"),
                };
                var grants = new Dictionary<string, int>(StringComparer.Ordinal);
                if (o.TryGet("grants", out var g) && g is JsonNode.Obj go)
                {
                    foreach (var kv in go.Fields)
                    {
                        grants[kv.Key] = kv.Value is JsonNode.Num n ? n.AsInt() : throw new FormatException(FileName + ": " + sku + " grant " + kv.Key + " is not a number");
                    }
                }

                var ents = new List<string>();
                if (o.TryGet("entitlements", out var e) && e is JsonNode.Arr ea)
                {
                    foreach (var item in ea.Items)
                    {
                        ents.Add(item is JsonNode.Str s ? s.Value : throw new FormatException(FileName + ": " + sku + " entitlement is not a string"));
                    }
                }

                if (map.ContainsKey(sku))
                {
                    throw new FormatException(FileName + ": duplicate sku " + sku);
                }

                map[sku] = new IapReward(sku, type, grants, ents);
            }

            return new IapRewardCatalog(LayoutJson.OptStr(root, "package_name", ""), map);
        }

        /// <summary>Every theme-pack product with its store type. Products missing from the reward table are left out (they cannot be granted).</summary>
        public IReadOnlyList<IapProduct> ProductsFor(IEnumerable<Store.ProductDef> products)
        {
            var list = new List<IapProduct>();
            foreach (var p in products)
            {
                var r = Find(p.Sku);
                if (r != null)
                {
                    list.Add(new IapProduct(p.Sku, p.ProductId, r.Type));
                }
            }

            return list;
        }
    }
}
