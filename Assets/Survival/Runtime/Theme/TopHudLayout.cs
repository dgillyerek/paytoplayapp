#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Theme
{
    /// <summary>
    /// The top of the runner and battle HUDs (Derek 2026-10-28: "put them [hero health] in the top left. make the level
    /// banner smaller and put the remaining distance next to it"). Two rows: a slim top row (button, small title banner,
    /// distance / Retreat, kills) and, in the runner, a badge row on the right; the hero HP panel hangs from
    /// <see cref="PanelTop"/> on the left under the top row. Boxes are anchor shares of the canvas (x of width, y of height).
    /// </summary>
    public static class TopHudLayout
    {
        public readonly struct Box
        {
            public readonly string Name;
            public readonly float X0, Y0, X1, Y1;

            public Box(string name, float x0, float y0, float x1, float y1)
            {
                Name = name;
                X0 = x0;
                Y0 = y0;
                X1 = x1;
                Y1 = y1;
            }

            public bool Overlaps(Box o) => X0 < o.X1 && o.X0 < X1 && Y0 < o.Y1 && o.Y0 < Y1;

            public override string ToString() => $"{Name} [{X0:0.###},{Y0:0.###} - {X1:0.###},{Y1:0.###}]";
        }

        /// <summary>Top row (both scenes).</summary>
        public const float RowY0 = 0.937f, RowY1 = 0.985f;

        /// <summary>Runner badge row, right of the hero panel.</summary>
        public const float BadgeY0 = 0.892f, BadgeY1 = 0.929f;

        /// <summary>Top edge of the hero panel as a share of the canvas height (just under the top row).</summary>
        public const float PanelTop = 0.931f;

        // Runner top row.
        public static readonly Box Leave = new Box("Leave", 0.015f, RowY0, 0.125f, RowY1);
        public static readonly Box RunnerTitle = new Box("RunnerTitle", 0.135f, RowY0, 0.425f, RowY1);
        public static readonly Box Distance = new Box("Distance", 0.435f, 0.946f, 0.775f, 0.976f);
        public static readonly Box BossMark = new Box("BossMark", 0.755f, RowY0, 0.805f, RowY1);
        public static readonly Box Kills = new Box("Kills", 0.815f, 0.942f, 0.985f, 0.980f);

        // Battle top row.
        public static readonly Box Clock = new Box("Clock", 0.015f, RowY0, 0.135f, RowY1);
        public static readonly Box BattleTitle = new Box("BattleTitle", 0.145f, RowY0, 0.765f, RowY1);
        public static readonly Box Retreat = new Box("Retreat", 0.775f, 0.940f, 0.985f, 0.982f);

        /// <summary>The runner's steer hint (the bottom of the screen is free again).</summary>
        public static readonly Box Hint = new Box("Hint", 0f, 0.10f, 1f, 0.15f);

        /// <summary>The runner's pop-up banner, under a full panel (5 heroes + 4 follower types).</summary>
        public static readonly Box RunnerBanner = new Box("Banner", 0.08f, 0.56f, 0.92f, 0.625f);

        public static IReadOnlyList<Box> Runner(float badgeLeft, float badgeWidth, float speedLeft) => new[]
        {
            Leave, RunnerTitle, Distance, BossMark, Kills,
            new Box("DmgBadge", badgeLeft, BadgeY0, badgeLeft + badgeWidth, BadgeY1),
            new Box("SpdBadge", speedLeft, BadgeY0, speedLeft + badgeWidth, BadgeY1),
            RunnerBanner,
            Hint,
        };

        public static IReadOnlyList<Box> Battle() => new[] { Clock, BattleTitle, Retreat };

        /// <summary>The battle's enemy panel: the hero panel mirrored to the top-right.</summary>
        public static Box EnemyPanel(float width, float height, int enemies)
        {
            var p = Panel(width, height, enemies);
            return new Box("EnemyPanel", 1f - p.X1, p.Y0, 1f - p.X0, p.Y1);
        }

        /// <summary>The hero panel's box on a canvas <paramref name="width"/> x <paramref name="height"/> reference units.</summary>
        public static Box Panel(float width, float height, int heroes, int unitTypes = 0)
        {
            var x0 = HeroHpPanelLayout.Margin / width;
            var x1 = (HeroHpPanelLayout.Margin + HeroHpPanelLayout.Width(width)) / width;
            var y1 = PanelTop;
            var y0 = y1 - (HeroHpPanelLayout.Height(heroes, unitTypes) / Math.Max(1f, height));
            return new Box("HeroPanel", x0, y0, x1, y1);
        }
    }
}
