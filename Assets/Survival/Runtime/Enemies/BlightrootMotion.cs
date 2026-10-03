namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Blightroot creature-pack catalog and 1080×1920 Game-view framing.
    /// Motion FBX are native Mixamo: AnimationStack <see cref="TakeName"/>, root bone
    /// <see cref="BoneRoot"/>, no Armature. Play those takes on the Dual Weapon Combo
    /// skinned body. Do not retarget, mirror, or time-reverse. Reject an empty take
    /// (the Blender-wrapped Scene stack is not these files). HOLD merge.
    /// </summary>
    public static class BlightrootMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/enemies/3d/blightroot";
        public const string ThemePackCreatureDir = ThemePackDir + "/creature_pack";
        public const string BodyFileName = "Blightroot_Dual_Weapon_Combo.fbx";
        public const string BodyMd5 = "ab0132afa21a4106ae3503042a7a2b86";
        public const string BodyThemePackRel = ThemePackDir + "/" + BodyFileName;

        /// <summary>Verified AnimationStack on every attached FBX. Not Scene.</summary>
        public const string TakeName = "mixamo.com";

        /// <summary>Empty on these files. Importing it freezes the body. Reject it.</summary>
        public const string RejectedEmptyTakeName = "Scene";

        public const string BoneRoot = "mixamorig:Hips";
        public const int ClipCount = 19;
        public const float MinClipSeconds = 0.02f;
        public const bool Mirror = false;

        public const float GameViewWidth = 1080f;
        public const float GameViewHeight = 1920f;

        /// <summary>
        /// Front Game camera. Pulled back so jumps, forward root motion, and the
        /// fallen dying pose stay inside the portrait frame under the top dropdown.
        /// </summary>
        public const float PlayCamFovDegrees = 54f;
        public const float PlayCamX = 0f;
        public const float PlayCamY = 0.9f;
        public const float PlayCamZ = 10.75f;
        public const float PlayCamLookX = 0f;
        public const float PlayCamLookY = 1.3f;
        public const float PlayCamLookZ = 0.2f;
        public const float PlayCamNear = 0.25f;
        public const float PlayCamFar = 40f;

        public const double MeshMinX = -0.826026;
        public const double MeshMinY = -0.950091;
        public const double MeshMinZ = -0.390553;
        public const double MeshMaxX = 0.827687;
        public const double MeshMaxY = 0.950241;
        public const double MeshMaxZ = 0.377573;
        public const double BindHipX = 0.0008305013179779053;
        public const double BindHipY = -0.02454525977373123;
        public const double BindHipZ = 0.030744770541787148;
        public const double LimbMarginX = 0.45;
        public const double LimbMarginY = 0.55;
        public const double LimbMarginZ = 0.55;
        public const double DyingSprawlMeters = 0.90;
        public const double JumpHeadroomMeters = 0.25;

        /// <summary>NDC limits that leave the top dropdown and a bottom caption clear.</summary>
        public const double GameViewNdcXLimit = 0.90;
        public const double GameViewNdcYMax = 0.62;
        public const double GameViewNdcYMin = -0.88;

        public readonly struct Clip
        {
            public Clip(
                string exactName,
                string fileName,
                string md5,
                bool loop,
                float lastFrame,
                float hipXMin,
                float hipXMax,
                float hipYMin,
                float hipYMax,
                float hipZMin,
                float hipZMax)
            {
                ExactName = exactName;
                FileName = fileName;
                Md5 = md5;
                Loop = loop;
                LastFrame = lastFrame;
                HipXMin = hipXMin;
                HipXMax = hipXMax;
                HipYMin = hipYMin;
                HipYMax = hipYMax;
                HipZMin = hipZMin;
                HipZMax = hipZMax;
            }

            public string ExactName { get; }
            public string FileName { get; }
            public string Md5 { get; }
            public bool Loop { get; }
            public float LastFrame { get; }
            public float HipXMin { get; }
            public float HipXMax { get; }
            public float HipYMin { get; }
            public float HipYMax { get; }
            public float HipZMin { get; }
            public float HipZMax { get; }

            public string ThemePackRel => ThemePackCreatureDir + "/" + FileName;
        }

        /// <summary>Dropdown order. Ordinal-exact Mixamo titles, not filenames.</summary>
        public static readonly Clip[] Clips =
        {
            new("mutant right turn 90", "Blightroot_mutant_right_turn_90.fbx", "deb903148fc6d4163f433a6d5cf695aa", false, 72f,
                0.003f, 0.222f, 0.775f, 0.818f, -0.104f, 0.036f),
            new("mutant jumping", "Blightroot_mutant_jumping.fbx", "8b481600d4de39f418272733a7ae1383", false, 95f,
                -0.025f, 0.063f, 0.537f, 1.147f, -0.021f, 0.104f),
            new("mutant jumping (2)", "Blightroot_mutant_jumping_(2).fbx", "892882fdd45ea897a10a962ed697d705", false, 18f,
                -0.027f, 0.013f, 0.834f, 1.147f, -0.001f, 0.015f),
            new("mutant walking", "Blightroot_mutant_walking.fbx", "d709a26d5dae100a0cb78381dd02ec0f", true, 43f,
                0.019f, 0.081f, 0.76f, 0.883f, -0.004f, 1.449f),
            new("mutant swiping", "Blightroot_mutant_swiping.fbx", "e3fb398dfd2f4d30eb76400c5172a1fa", false, 80f,
                -0.147f, 0.101f, 0.621f, 0.829f, -0.217f, 0.593f),
            new("mutant run", "Blightroot_mutant_run.fbx", "606f0c8dd535db31c8656ae83381247a", true, 26f,
                -0.012f, 0.045f, 0.688f, 0.889f, 0.014f, 1.61f),
            new("mutant dying", "Blightroot_mutant_dying.fbx", "1b90590cef60c6c9c26f3a96348391a8", false, 138f,
                -0.051f, 0.005f, 0.071f, 0.792f, -0.85f, 0.002f),
            new("mutant idle", "Blightroot_mutant_idle.fbx", "0a457bce0f21fafd8f1ab376e5b17400", true, 427f,
                -0.002f, 0.019f, 0.791f, 0.817f, -0.038f, 0.044f),
            new("mutant breathing idle", "Blightroot_mutant_breathing_idle.fbx", "8092732e6d4d01ffea687ce374b52f82", true, 121f,
                0.003f, 0.007f, 0.791f, 0.802f, -0.02f, 0.004f),
            new("mutant right turn 45", "Blightroot_mutant_right_turn_45.fbx", "a7f21fea83dbf25c489a6cb69fcc89b5", false, 59f,
                -0.012f, 0.145f, 0.767f, 0.827f, -0.126f, 0.008f),
            new("mutant idle (2)", "Blightroot_mutant_idle_(2).fbx", "422ab26d0a6760b38284ce6f57da51d7", true, 159f,
                0.002f, 0.017f, 0.787f, 0.817f, -0.018f, 0.044f),
            new("mutant right turn 45 (2)", "Blightroot_mutant_right_turn_45_(2).fbx", "fd21a7f1e4bc393ac4ef838062d8901e", false, 40f,
                -0.045f, 0.134f, 0.784f, 0.831f, -0.084f, -0.008f),
            new("mutant left turn 45", "Blightroot_mutant_left_turn_45.fbx", "e495650b1aebb115a10f0f4acd9b008d", false, 40f,
                -0.128f, 0.052f, 0.785f, 0.833f, -0.087f, -0.011f),
            new("left turn 45", "Blightroot_left_turn_45.fbx", "b0ee9b944655f88f16e6f0791dcba683", false, 40f,
                -0.132f, 0.004f, 0.785f, 0.833f, -0.098f, 0.013f),
            new("mutant flexing muscles", "Blightroot_mutant_flexing_muscles.fbx", "18af4e078e3fa191774f05c2d2f304af", true, 140f,
                -0.016f, 0.085f, 0.706f, 0.827f, -0.03f, 0.068f),
            new("jump attack", "Blightroot_jump_attack.fbx", "0216761cbb28709be2ac604a00cb0acb", false, 114f,
                -0.001f, 0.141f, 0.46f, 1.912f, -0.018f, 1.83f),
            new("mutant jump attack", "Blightroot_mutant_jump_attack.fbx", "4a336a7a635925edd594be5f123e064a", false, 111f,
                -0.002f, 0.132f, 0.465f, 1.912f, -0.018f, 1.443f),
            new("mutant punch", "Blightroot_mutant_punch.fbx", "4ed240ee7e79a246ed06848808f19fd6", false, 33f,
                -0.023f, 0.041f, 0.641f, 0.791f, -0.045f, 0.141f),
            new("mutant roaring", "Blightroot_mutant_roaring.fbx", "26822fb79783425baf6d6fa4eac80f56", true, 162f,
                -0.04f, 0.157f, 0.677f, 0.793f, -0.069f, 0.228f),
        };

        public static bool TryGet(string exactName, out Clip clip)
        {
            foreach (var candidate in Clips)
            {
                if (string.Equals(candidate.ExactName, exactName, System.StringComparison.Ordinal))
                {
                    clip = candidate;
                    return true;
                }
            }

            clip = default;
            return false;
        }

        public static bool IsEmptyClip(float lengthSeconds) =>
            float.IsNaN(lengthSeconds) || lengthSeconds < MinClipSeconds;

        public static bool GameViewFitsEveryClip()
        {
            foreach (var clip in Clips)
            {
                if (!GameViewFitsClip(clip))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool GameViewFitsClip(Clip clip)
        {
            BodyAabb(clip, out var minX, out var minY, out var minZ, out var maxX, out var maxY, out var maxZ);
            var xs = new[] { minX, maxX };
            var ys = new[] { minY, maxY };
            var zs = new[] { minZ, maxZ };
            foreach (var x in xs)
            {
                foreach (var y in ys)
                {
                    foreach (var z in zs)
                    {
                        if (!GameViewContains(x, y, z))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public static void BodyAabb(
            Clip clip,
            out double minX,
            out double minY,
            out double minZ,
            out double maxX,
            out double maxY,
            out double maxZ)
        {
            minX = MeshMinX + (clip.HipXMin - BindHipX) - LimbMarginX;
            maxX = MeshMaxX + (clip.HipXMax - BindHipX) + LimbMarginX;
            minY = MeshMinY + (clip.HipYMin - BindHipY) - LimbMarginY;
            maxY = MeshMaxY + (clip.HipYMax - BindHipY) + LimbMarginY;
            minZ = MeshMinZ + (clip.HipZMin - BindHipZ) - LimbMarginZ;
            maxZ = MeshMaxZ + (clip.HipZMax - BindHipZ) + LimbMarginZ;
            if (string.Equals(clip.ExactName, "mutant dying", System.StringComparison.Ordinal))
            {
                minX -= DyingSprawlMeters;
                maxX += DyingSprawlMeters;
                minZ -= DyingSprawlMeters;
                maxZ += DyingSprawlMeters;
            }

            if (clip.ExactName.IndexOf("jump", System.StringComparison.Ordinal) >= 0)
            {
                maxY += JumpHeadroomMeters;
            }
        }

        public static bool GameViewContains(double worldX, double worldY, double worldZ)
        {
            double fx = PlayCamLookX - PlayCamX;
            double fy = PlayCamLookY - PlayCamY;
            double fz = PlayCamLookZ - PlayCamZ;
            var fl = System.Math.Sqrt(fx * fx + fy * fy + fz * fz);
            if (fl < 1e-8)
            {
                return false;
            }

            fx /= fl;
            fy /= fl;
            fz /= fl;
            var rx = fz;
            var ry = 0d;
            var rz = -fx;
            var rl = System.Math.Sqrt(rx * rx + ry * ry + rz * rz);
            rx /= rl;
            ry /= rl;
            rz /= rl;
            var ux = fy * rz - fz * ry;
            var uy = fz * rx - fx * rz;
            var uz = fx * ry - fy * rx;
            var ul = System.Math.Sqrt(ux * ux + uy * uy + uz * uz);
            ux /= ul;
            uy /= ul;
            uz /= ul;

            var dx = worldX - PlayCamX;
            var dy = worldY - PlayCamY;
            var dz = worldZ - PlayCamZ;
            var cz = dx * fx + dy * fy + dz * fz;
            if (cz < PlayCamNear)
            {
                return false;
            }

            var cx = dx * rx + dy * ry + dz * rz;
            var cy = dx * ux + dy * uy + dz * uz;
            var tanV = System.Math.Tan(PlayCamFovDegrees * System.Math.PI / 360d);
            var tanH = tanV * (GameViewWidth / GameViewHeight);
            var nx = (cx / cz) / tanH;
            var ny = (cy / cz) / tanV;
            return System.Math.Abs(nx) <= GameViewNdcXLimit
                   && ny <= GameViewNdcYMax
                   && ny >= GameViewNdcYMin;
        }
    }
}
