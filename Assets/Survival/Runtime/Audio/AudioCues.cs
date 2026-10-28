#nullable enable
namespace Survival.Domain.Audio
{
    /// <summary>
    /// Stable sound cue ids the game code asks for. A theme pack's audio config (pack.json "audio") maps each id to a
    /// file and its mix settings, so another pack can ship different sounds without code changes.
    /// </summary>
    public static class AudioCues
    {
        public const string MusicVillage = "music.village";
        public const string MusicRunner = "music.runner";
        public const string MusicBattle = "music.battle";

        public const string BossIncoming = "sting.boss_incoming";
        public const string JingleWin = "jingle.win";
        public const string JingleLose = "jingle.lose";

        public const string EnemyDeath = "sfx.enemy.death";
        public const string EnemyDeathBig = "sfx.enemy.death_big";
        public const string HeroHurt = "sfx.hero.hurt";
        public const string HeroDeath = "sfx.hero.death";

        public const string UiTap = "ui.tap";
        public const string UiDeny = "ui.deny";
        public const string UiStoreOpen = "ui.store_open";
        public const string UiGift = "ui.gift";
        public const string UiPurchaseSuccess = "ui.purchase_success";

        public const string CollectResource = "eco.collect.resource";
        public const string BuildComplete = "eco.build_complete";
        public const string UpgradeStart = "eco.upgrade_start";

        /// <summary>Every cue id the code names directly (the config may add more for its event maps).</summary>
        public static readonly string[] CodeCues =
        {
            MusicVillage, MusicRunner, MusicBattle, BossIncoming, JingleWin, JingleLose, EnemyDeath, EnemyDeathBig,
            HeroHurt, HeroDeath, UiTap, UiDeny, UiStoreOpen, UiGift, UiPurchaseSuccess, CollectResource, BuildComplete,
            UpgradeStart
        };
    }
}
