using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Stormcrest Theme A attack add-on (Design 2026-10-07). Storm Bolt — jagged blue-white lightning from the beak (2.6 m reach, f14–20) with gold sparks.
    /// New 0–30 frame Scene take at 30 fps on the existing blenderig (same bones, no rebind; rest and walk unchanged).
    /// Frames below are baked from Design attack_meta drivers in Unity convention (character forward = +Z).
    /// Prop VFX meshes are look reference only; Unity draws its own particles. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class StormcrestAttack
    {
        public const string FileName = "STORMCREST_blenderig_attack.fbx";
        public const string FileMd5 = "48acf6540852f78175b2cda1a89b4ceb";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/stormcrest/attack_20261007";
        public const int ReleaseFrame = 14;
        public const bool InPlace = false;

        /// <summary>Prop FBXs copied next to the character FBXs, with Design MD5s.</summary>
        public static readonly string[] PropFileNames =
        {

        };

        public static readonly string[] PropFileMd5s =
        {

        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Stormcrest",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { -0.0234f, 0.62743f, 0.60981f, 0f, 0.99541f, 0.74974f, -0.84536f, 0.81137f, -0.05129f, 0.80947f, 0.73399f, 0.61217f },
            new[]
            {
                new AttackTrack(
                    "STORMCREST_stormbolt_vfx",
                    AttackTrackKind.Projectile,
                    "",
                    "",
                    0,
                    14,
                    4f,
                    AttackVfxShape.Lightning,
                    0.3f,
                    2.76f,
                    new[] { 0.93092f, 0.96858f, 1f },
                    new[] { 0.3133f, 0.58383f, 1f },
                    new[] { 1f, 0.86504f, 0.58383f },
                    new[] { 0.55285f, 0.53753f, 2.76231f },
                    new[]
                    {
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 0.75444f, 0f, 0f, 0f, 1f, 0.35f, true),
                        new AttackFrame(-0.0598f, 0.88144f, 0.88777f, 0f, 0f, 0f, 1f, 0.56667f, true),
                        new AttackFrame(-0.0598f, 0.88144f, 1.02111f, 0f, 0f, 0f, 1f, 0.78333f, true),
                        new AttackFrame(-0.0598f, 0.88144f, 1.15444f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.0598f, 0.88144f, 1.28777f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.0598f, 0.88144f, 1.42111f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.0598f, 0.88144f, 1.55444f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.0598f, 0.88144f, 1.68777f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 1.82111f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 1.95444f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 2.08777f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 2.22111f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 2.35444f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 2.48777f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 2.62111f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 2.75444f, 0f, 0f, 0f, 1f, 1f, false),
                        new AttackFrame(-0.0598f, 0.88144f, 2.88777f, 0f, 0f, 0f, 1f, 1f, false)
                    },
                    "storm bolt anchored at beak vertex #35320; lightning strikes forward (2.6m reach), visible ~6 frames")
            });
    }
}
