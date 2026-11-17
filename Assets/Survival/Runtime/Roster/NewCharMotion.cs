using System;
using System.Collections.Generic;

namespace Survival.Domain.Roster
{
    /// <summary>One authored clip of a Design character in <c>preview_new_chars</c>.</summary>
    public sealed class NewCharClip
    {
        public NewCharClip(string pose, float seconds, int contactFrame = -1, BattleImpactKind impact = BattleImpactKind.SwordSteel, string label = "")
        {
            Pose = pose;
            Seconds = seconds;
            ContactFrame = contactFrame;
            Impact = impact;
            Label = label.Length > 0 ? label : pose;
        }

        /// <summary>Pose name. Also the FBX suffix: PREFIX_<c>Pose</c>.fbx.</summary>
        public string Pose { get; }

        /// <summary>Clip length in seconds, from the FBX frame range (Blender 4.2 import).</summary>
        public float Seconds { get; }

        /// <summary>Hit frame at 30 fps from the clip's first frame. -1 for idle and walk.</summary>
        public int ContactFrame { get; }

        public BattleImpactKind Impact { get; }

        public string Label { get; }

        public float ContactSeconds => ContactFrame / NewCharMotion.FrameRate;

        public bool IsAttack => ContactFrame >= 0;
    }

    /// <summary>
    /// A Design character whose body, clips, atlas, cape and props live in one folder under
    /// <c>ThemePack/fantasy_kingdom_a/art/preview_new_chars/</c> (Seraphine, Kaelen, Malzareth). This only names the
    /// files and the frames. Nothing here rewrites an FBX, re-rigs or re-bakes a clip.
    /// </summary>
    public sealed class NewCharSpec
    {
        public NewCharSpec(
            string name,
            string folder,
            string prefix,
            BattleSide side,
            float designHeightMetres,
            float walkSpeedMetresPerSecond,
            float attackRangeMetres,
            string strikeBone,
            IReadOnlyList<NewCharClip> attacks,
            BattleHitBox box)
        {
            Name = name;
            Folder = folder;
            Prefix = prefix;
            Side = side;
            DesignHeightMetres = designHeightMetres;
            WalkSpeedMetresPerSecond = walkSpeedMetresPerSecond;
            AttackRangeMetres = attackRangeMetres;
            StrikeBone = strikeBone;
            Attacks = attacks;
            Box = box;
        }

        /// <summary>Roster name (<see cref="FighterNames"/>).</summary>
        public string Name { get; }

        /// <summary>Folder under preview_new_chars (lower case).</summary>
        public string Folder { get; }

        /// <summary>File prefix (upper case), e.g. SERAPHINE.</summary>
        public string Prefix { get; }

        public BattleSide Side { get; }
        public float DesignHeightMetres { get; }
        public float WalkSpeedMetresPerSecond { get; }
        public float AttackRangeMetres { get; }

        /// <summary>The weapon hand: the bone the melee hit lands from.</summary>
        public string StrikeBone { get; }

        /// <summary>The battle rotation, attack 1 first. Every clip is one Derek has seen in the New Characters Preview.</summary>
        public IReadOnlyList<NewCharClip> Attacks { get; }

        public BattleHitBox Box { get; }

        public NewCharClip Primary => Attacks[0];

        public string ThemePackDir => NewCharMotion.ThemePackRoot + "/" + Folder;

        public string RigRestRel => ThemePackDir + "/" + Prefix + "_rig_rest.fbx";
        public string AtlasRel => ThemePackDir + "/" + Prefix + "_atlas_2048.png";
        public string CapeRel => ThemePackDir + "/" + Prefix + "_cape.fbx";
        public string ClipRel(string pose) => ThemePackDir + "/" + Prefix + "_" + pose + ".fbx";

        /// <summary>Every pose the actor answers to: rest (= idle frame 0 onward), idle, walk, then the attacks.</summary>
        public IEnumerable<string> PoseNames
        {
            get
            {
                yield return NewCharMotion.RestPoseName;
                yield return NewCharMotion.IdlePoseName;
                yield return NewCharMotion.WalkPoseName;
                foreach (var a in Attacks)
                {
                    yield return a.Pose;
                }
            }
        }

        public NewCharClip? Attack(string pose)
        {
            foreach (var a in Attacks)
            {
                if (string.Equals(a.Pose, pose, StringComparison.Ordinal))
                {
                    return a;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Seraphine (spear hero), Kaelen (dual-sword hero) and Malzareth (greatsword villain), Design 2026-11-03 drops.
    /// Hero vs villain follows Design's folders (heroes/anim/seraphine, heroes/anim/kaelen, enemies/anim/malzareth).
    /// Clip lengths are the FBX frame ranges; contact frames are where the weapon hand moves fastest on the strike,
    /// measured in Blender 4.2 on the files in preview_new_chars (2026-11-15). The attacks are the ones the New
    /// Characters Preview plays (Seraphine: armada, thrust slash; Kaelen: dual slash, inward slash; Malzareth: greatsword,
    /// great sword casting, standing melee). Vorgath keeps his own <see cref="Enemies.VorgathMotion"/>.
    /// </summary>
    public static class NewCharMotion
    {
        public const string ThemePackRoot = "ThemePack/fantasy_kingdom_a/art/preview_new_chars";
        public const float FrameRate = 30f;

        public const string RestPoseName = "rest";
        public const string IdlePoseName = "idle";
        public const string WalkPoseName = "walk";

        /// <summary>
        /// Seraphine. 1.79 m with her wings. Armada: frames 4-53 (1.63 s), the spear hand fastest on Blender frame 37.
        /// Thrust slash: frames 1-54 at 25 fps (2.12 s), the lunge lands on Blender frame 23 (0.88 s).
        /// </summary>
        public static readonly NewCharSpec Seraphine = new NewCharSpec(
            FighterNames.Seraphine,
            "seraphine",
            "SERAPHINE",
            BattleSide.Hero,
            1.79f,
            1.6f,
            1.8f,
            "mixamorig:RightHand",
            new[]
            {
                new NewCharClip("attack_armada", 49f / FrameRate, 33, BattleImpactKind.SwordSteel, "armada"),
                new NewCharClip("attack_thrust_slash", 53f / 25f, 26, BattleImpactKind.SwordSteel, "spear thrust")
            },
            new BattleHitBox(0.30f, 0.25f, 0.22f, 1.80f));

        /// <summary>
        /// Kaelen. 1.85 m. Dual slash: frames 2-111 (3.63 s), both blades fastest on Blender frame 56 (the middle of the
        /// three cuts). Inward slash: frames 2-44 (1.41 s), the blades cross on frame 26. The swords are the separate
        /// KAELEN_sword_L/R props on LeftArm.002 / RightArm.002 (his hands).
        /// </summary>
        public static readonly NewCharSpec Kaelen = new NewCharSpec(
            FighterNames.Kaelen,
            "kaelen",
            "KAELEN",
            BattleSide.Hero,
            1.85f,
            1.55f,
            1.4f,
            "mixamorig:RightArm.002",
            new[]
            {
                new NewCharClip("attack_dualslash", 109f / FrameRate, 54, BattleImpactKind.SwordSteel, "dual slash"),
                new NewCharClip("attack2_inwardslash", 42.3f / FrameRate, 24, BattleImpactKind.SwordSteel, "inward slash")
            },
            new BattleHitBox(0.28f, 0.25f, 0.20f, 1.85f));

        /// <summary>
        /// Malzareth. 2.0 m with the greatsword. Greatsword: frames 2-57 (1.83 s), sword hand fastest on frame 33.
        /// Great sword casting (the two-handed overhead chop): frames 2-52 (1.67 s), the chop lands on frame 28.
        /// Standing melee: frames 2-74 (2.4 s), the swing lands on frame 32. His right hand rides RightForeArm.001
        /// (Derek's 20-bone rig has no RightHand).
        /// </summary>
        public static readonly NewCharSpec Malzareth = new NewCharSpec(
            FighterNames.Malzareth,
            "malzareth",
            "MALZARETH",
            BattleSide.Villain,
            2.0f,
            1.45f,
            1.7f,
            "mixamorig:RightForeArm.001",
            new[]
            {
                new NewCharClip("attack_greatsword", 55f / FrameRate, 31, BattleImpactKind.SwordSteel, "greatsword cleave"),
                new NewCharClip("attack_great_sword_casting", 50f / FrameRate, 26, BattleImpactKind.GroundSlam, "overhead chop"),
                new NewCharClip("attack_standing_melee", 72f / FrameRate, 30, BattleImpactKind.SwordSteel, "greatsword swing")
            },
            new BattleHitBox(0.34f, 0.30f, 0.26f, 2.0f));

        public static IReadOnlyList<NewCharSpec> All { get; } = new[] { Seraphine, Kaelen, Malzareth };

        public static bool TryFind(string name, out NewCharSpec spec)
        {
            foreach (var s in All)
            {
                if (string.Equals(s.Name, name, StringComparison.Ordinal))
                {
                    spec = s;
                    return true;
                }
            }

            spec = null!;
            return false;
        }
    }
}
