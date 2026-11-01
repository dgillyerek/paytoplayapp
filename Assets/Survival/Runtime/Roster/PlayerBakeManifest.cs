#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Enemies;
using Survival.Domain.Heroes;

namespace Survival.Domain.Roster
{
    /// <summary>
    /// What a phone (player) build must carry for every Locked Battle / runner fighter. The Editor builds each
    /// character from "Assets/ThemePack/..." files through AssetDatabase; a player has no AssetDatabase, so the
    /// Editor bake (Survival/Player Build/Bake Player Assets) references these files from a Resources registry.
    /// This list is the floor the build preprocessor checks. The bake also adds every path the Editor warm-up
    /// actually loads (each fighter spawned, every pose played), so a file missed here still ships.
    /// Paths are relative to Assets/ (ThemePack-rel), forward slashes.
    /// </summary>
    public static class PlayerBakeManifest
    {
        /// <summary>Raw files (JSON catalogs, layouts, PNG art the views decode with LoadImage) are copied as bytes from here.</summary>
        public static readonly string[] RawFileRoots =
        {
            "ThemePack/fantasy_kingdom_a",
            "Survival/Resources/Survival"
        };

        /// <summary>Model folders: imported as assets (FBX, atlases), never shipped as raw bytes.</summary>
        public static readonly string[] RawFileExcludedDirs =
        {
            "ThemePack/fantasy_kingdom_a/art/heroes/3d",
            "ThemePack/fantasy_kingdom_a/art/enemies/3d"
        };

        public static readonly string[] RawFileExtensions = { ".json", ".png", ".jpg", ".jpeg", ".txt" };

        /// <summary>Theme sound folder (audio.json lists the cues): every clip here is baked as an imported AudioClip.</summary>
        public const string AudioDir = "ThemePack/fantasy_kingdom_a/audio";

        public static readonly string[] AudioExtensions = { ".ogg", ".wav" };

        public static bool IsAudioClip(string assetsRelPath)
        {
            var p = assetsRelPath.Replace('\\', '/');
            if (!p.StartsWith(AudioDir + "/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (var ext in AudioExtensions)
            {
                if (p.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Sir Aldric pilot paths (Survival.Unity SirAldricMeshyAnimateActor reads them; duplicated here so the domain test sees them).</summary>
        public const string SirAldricPilotDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot";
        public const string SirAldricWalkFbx = SirAldricPilotDir + "/SirAldric_body_holefixed_walk.fbx";
        public const string SirAldricAttackFbx = SirAldricPilotDir + "/SirAldric_body_holefixed_slash_SWORD_SHIELD_ATTACK.fbx";
        public const string SirAldricSwordFbx = SirAldricPilotDir + "/SirAldric_PILOT_sword.fbx";
        public const string SirAldricCapeFbx = SirAldricPilotDir + "/SirAldric_PILOT_cape.fbx";
        public const string SirAldricTextureDir = SirAldricPilotDir + "/mixamo_tex";

        public static readonly string[] SirAldricTextures =
        {
            SirAldricTextureDir + "/Meshy_AI_Lionheart_Sentinel_0929004215_texture.png",
            SirAldricTextureDir + "/Meshy_AI_Lionheart_Sentinel_0929004215_texture_metallic.png",
            SirAldricTextureDir + "/Meshy_AI_Lionheart_Sentinel_0929004215_texture_roughness.png"
        };

        public sealed class Fighter
        {
            public Fighter(string name, string modelDir, IReadOnlyList<string> assets, IReadOnlyList<string> rawFiles)
            {
                Name = name;
                ModelDir = modelDir;
                Assets = assets;
                RawFiles = rawFiles;
            }

            public string Name { get; }

            /// <summary>Folder that holds the fighter's FBX files.</summary>
            public string ModelDir { get; }

            /// <summary>Imported assets (FBX prefabs with their clips / avatars / meshes, textures) loaded by path.</summary>
            public IReadOnlyList<string> Assets { get; }

            /// <summary>Files read as raw bytes (File.ReadAllBytes + LoadImage) at runtime.</summary>
            public IReadOnlyList<string> RawFiles { get; }
        }

        private static IReadOnlyList<Fighter>? _all;

        /// <summary>One entry per <see cref="LockedBattleRoster.All"/> fighter, same order.</summary>
        public static IReadOnlyList<Fighter> All => _all ??= Build();

        public static Fighter? For(string name)
        {
            foreach (var f in All)
            {
                if (string.Equals(f.Name, name, StringComparison.Ordinal))
                {
                    return f;
                }
            }

            return null;
        }

        /// <summary>Every asset path of every fighter, de-duplicated, in a stable order.</summary>
        public static IReadOnlyList<string> AllAssets()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<string>();
            foreach (var f in All)
            {
                foreach (var a in f.Assets)
                {
                    if (seen.Add(a))
                    {
                        list.Add(a);
                    }
                }
            }

            return list;
        }

        public static bool IsRawFile(string assetsRelPath)
        {
            var p = assetsRelPath.Replace('\\', '/');
            var underRoot = false;
            foreach (var root in RawFileRoots)
            {
                if (p.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
                {
                    underRoot = true;
                    break;
                }
            }

            if (!underRoot)
            {
                return false;
            }

            foreach (var dir in RawFileExcludedDirs)
            {
                if (p.StartsWith(dir + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            foreach (var ext in RawFileExtensions)
            {
                if (p.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<Fighter> Build()
        {
            var list = new List<Fighter>();
            foreach (var fighter in LockedBattleRoster.All)
            {
                list.Add(ForFighter(fighter.Name));
            }

            return list;
        }

        private static Fighter ForFighter(string name)
        {
            switch (name)
            {
                case "Emberfang":
                {
                    var d = EmberfangMotion.ThemePackDir;
                    var a = new List<string>
                    {
                        d + "/" + EmberfangMotion.RestFileName,
                        d + "/" + EmberfangMotion.FlapFileName,
                        d + "/" + EmberfangMotion.WalkFileName,
                        d + "/" + EmberfangMotion.AttackFileName,
                        d + "/emberfang_basecolor.png",
                        d + "/emberfang_normal.png",
                        d + "/emberfang_metallic.png",
                        d + "/emberfang_roughness.png"
                    };
                    AddTracks(a, d, EmberfangAttack.Spec);
                    return new Fighter(name, d, Dedup(a), Array.Empty<string>());
                }
                case "Ironhowl":
                {
                    var d = IronhowlMotion.ThemePackDir;
                    var a = new List<string>
                    {
                        IronhowlMotion.BodyThemePackRel,
                        IronhowlMotion.RestThemePackRel,
                        IronhowlMotion.WalkThemePackRel,
                        IronhowlMotion.AttackThemePackRel,
                        d + "/" + IronhowlMotion.Attack2FileName,
                        d + "/" + IronhowlMotion.Attack3FileName,
                        d + "/" + IronhowlMotion.BaseColorFileName,
                        d + "/" + IronhowlMotion.NormalFileName,
                        d + "/" + IronhowlMotion.MetallicFileName,
                        d + "/" + IronhowlMotion.RoughnessFileName
                    };
                    return new Fighter(name, d, Dedup(a), Array.Empty<string>());
                }
                case "Blightroot":
                {
                    var d = BlightrootMotion.ThemePackDir;
                    var a = new List<string>
                    {
                        BlightrootMotion.BodyThemePackRel,
                        d + "/Blightroot_basecolor.jpg",
                        d + "/Blightroot_metallicRoughness.jpg",
                        d + "/Blightroot_normal.jpg"
                    };
                    foreach (var clip in BlightrootMotion.Clips)
                    {
                        a.Add(clip.ThemePackRel);
                    }

                    return new Fighter(name, d, Dedup(a), Array.Empty<string>());
                }
                case "Sir Aldric":
                {
                    var a = new List<string>
                    {
                        SirAldricWalkFbx,
                        SirAldricAttackFbx,
                        SirAldricSwordFbx,
                        SirAldricCapeFbx,
                        SirAldric3DMotion.LiteSwordShieldThemePackDir + "/" + SirAldric3DMotion.LiteSwordShieldBodyFile
                    };
                    foreach (var clip in SirAldric3DMotion.LiteSwordShieldClips)
                    {
                        a.Add(clip.ThemePackRel);
                    }

                    foreach (var clip in SirAldric3DMotion.BattleExtraClips)
                    {
                        a.Add(clip.ThemePackRel);
                    }

                    a.AddRange(SirAldricTextures);
                    // The painted look PNGs are decoded from bytes (LoadPilotMap) as well as imported.
                    return new Fighter(name, SirAldricPilotDir, Dedup(a), SirAldricTextures);
                }
            }

            var found = SpecFor(name);
            if (found == null)
            {
                throw new InvalidOperationException("PlayerBakeManifest: no rig spec for roster fighter " + name);
            }

            var spec = found.Value;

            var assets = new List<string> { spec.RestThemePackRel, spec.ClipThemePackRel };
            foreach (var clip in spec.ExtraClips)
            {
                assets.Add(spec.ThemePackDir + "/" + clip.FileName);
            }

            if (!string.IsNullOrEmpty(spec.BaseColorFile))
            {
                assets.Add(spec.BaseColorThemePackRel);
            }

            if (!string.IsNullOrEmpty(spec.NormalFile))
            {
                assets.Add(spec.NormalThemePackRel);
            }

            if (!string.IsNullOrEmpty(spec.MetallicRoughnessFile))
            {
                assets.Add(spec.ThemePackDir + "/" + spec.TextureFolder + "/" + spec.MetallicRoughnessFile);
            }

            var attack = AttackFor(name);
            if (attack != null)
            {
                assets.Add(spec.ThemePackDir + "/" + attack.FileName);
                AddTracks(assets, spec.ThemePackDir, attack);
            }

            foreach (var prop in IdlePropsFor(name))
            {
                if (!string.IsNullOrEmpty(prop.PropFileName))
                {
                    assets.Add(spec.ThemePackDir + "/" + prop.PropFileName);
                }
            }

            switch (name)
            {
                case "Rowan":
                    assets.Add(spec.ThemePackDir + "/" + RowanCoat.FileName);
                    assets.Add(spec.ThemePackDir + "/" + RowanCoat.TextureFolder + "/" + RowanCoat.BaseColorFile);
                    assets.Add(spec.ThemePackDir + "/" + RowanCoat.TextureFolder + "/" + RowanCoat.NormalFile);
                    break;
                case "Bonequill":
                    assets.Add(spec.ThemePackDir + "/" + BonequillCape.FileName);
                    assets.Add(spec.ThemePackDir + "/" + BonequillCape.TextureFolder + "/" + BonequillCape.BaseColorFile);
                    assets.Add(spec.ThemePackDir + "/" + BonequillCape.TextureFolder + "/" + BonequillCape.NormalFile);
                    break;
            }

            return new Fighter(name, spec.ThemePackDir, Dedup(assets), Array.Empty<string>());
        }

        private static BlenderRigSpec? SpecFor(string name)
        {
            switch (name)
            {
                case "Stormcrest": return StormcrestMotion.Spec;
                case "Nightfang": return NightfangMotion.Spec;
                case "Lyra": return LyraMotion.Spec;
                case "Oakenshield": return OakenshieldMotion.Spec;
                case "Vespera": return VesperaMotion.Spec;
                case "Ashwyrm": return AshwyrmMotion.Spec;
                case "Rowan": return RowanMotion.Spec;
                case "Bonequill": return BonequillMotion.Spec;
                default: return null;
            }
        }

        private static BlenderRigAttackSpec? AttackFor(string name)
        {
            switch (name)
            {
                case "Stormcrest": return StormcrestAttack.Spec;
                case "Nightfang": return NightfangAttack.Spec;
                case "Lyra": return LyraAttack.Spec;
                case "Oakenshield": return OakenshieldAttack.Spec;
                case "Vespera": return VesperaAttack.Spec;
                case "Ashwyrm": return AshwyrmAttack.Spec;
                case "Rowan": return RowanAttack.Spec;
                case "Bonequill": return BonequillAttack.Spec;
                default: return null;
            }
        }

        private static BlenderRigIdlePropSpec[] IdlePropsFor(string name)
        {
            switch (name)
            {
                case "Lyra": return LyraMotion.IdleProps;
                case "Rowan": return RowanAttack.IdleProps;
                case "Bonequill": return BonequillAttack.IdleProps;
                default: return Array.Empty<BlenderRigIdlePropSpec>();
            }
        }

        private static void AddTracks(List<string> assets, string dir, BlenderRigAttackSpec attack)
        {
            foreach (var track in attack.Tracks)
            {
                if (track.HasMesh)
                {
                    assets.Add(dir + "/" + track.PropFileName);
                }
            }
        }

        private static IReadOnlyList<string> Dedup(List<string> items)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<string>();
            foreach (var i in items)
            {
                if (seen.Add(i))
                {
                    list.Add(i);
                }
            }

            return list;
        }
    }
}
