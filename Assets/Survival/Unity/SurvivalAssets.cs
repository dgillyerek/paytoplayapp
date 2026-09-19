using Survival.Domain.Ids;
using UnityEngine;

namespace Survival.Unity
{
    [CreateAssetMenu(menuName = "Survival/Theme Pack", fileName = "ThemePack")]
    public sealed class ThemePackAsset : ScriptableObject
    {
        public string themeId = SurvIds.ThemeIdFantasyKingdomA;
        public string systemsApi = SurvIds.SystemsApi;
        public string packPath = "ThemePack/fantasy_kingdom_a";
    }

    [CreateAssetMenu(menuName = "Survival/App Flavor", fileName = "AppFlavor")]
    public sealed class AppFlavorAsset : ScriptableObject
    {
        public string flavorId = SurvIds.FlavorIdFantasyKingdomA;
        public string themeId = SurvIds.ThemeIdFantasyKingdomA;
        public string systemsApi = SurvIds.SystemsApi;
        public string packPath = "ThemePack/fantasy_kingdom_a";
        public string bundleId = "com.paytoplay.fantasykingdoma";
        public string appName = "Kingdom Defense";
    }

    [CreateAssetMenu(menuName = "Survival/Catalog", fileName = "SurvivalCatalog")]
    public sealed class SurvivalCatalogAsset : ScriptableObject
    {
        public string systemsApi = SurvIds.SystemsApi;
        public TextAsset? dictionaryJson;
    }
}
