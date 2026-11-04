using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Campaign;
using Survival.Domain.Roster;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Review-only battle preview of the four Design characters of 2026-11-03 (Derek: "put the 4 new characters in a
    /// simple battle scene so I can see them"). Not in the roster, shop, campaign or runner, and deals no damage.
    /// Each character is one folder under art/preview_new_chars/: PREFIX_rig_rest.fbx (body + props), one FBX per clip,
    /// the atlas, and optionally PREFIX_cape.fbx + PREFIX_cape_1024.png + CAPE_ATTACH.md (cape_root under Spine2,
    /// 3 spring chains, capsule colliders read from the md table). Swap any file in place and replay.
    /// Derek: "nothing moves until their buttons are pressed. then they do the button movement until the next one.
    /// seemless transition between each". Everyone starts in their spot frozen on idle frame 0. A clip button (or keys 1-4)
    /// loops that clip for that character until another is pressed, crossfading from the current pose; attacks loop in
    /// place and spawn an impact on each contact. T turns everyone around, Q toggles the camera fly-around, A is the
    /// opt-in auto cycle, R freezes everyone back in their spots.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class NewCharsPreview : MonoBehaviour
    {
        public const string FolderName = "preview_new_chars";
        private const string Dir = "Assets/ThemePack/fantasy_kingdom_a/art/" + FolderName + "/";
        private const float Blend = 0.22f;
        private const float IdleGap = 1.2f;
        private const float HitAt = 0.5f;

        private sealed class Def
        {
            public Def(string folder, string prefix, bool hero, float z, float stopX, string[] clips, BattleImpactKind[] impacts, Color core, Color glow)
            {
                Folder = folder; Prefix = prefix; Hero = hero; Z = z; StopX = stopX; Clips = clips; Impacts = impacts; Core = core; Glow = glow;
            }

            public string Folder, Prefix;
            public bool Hero;
            public float Z, StopX;
            public string[] Clips;
            public BattleImpactKind[] Impacts;
            public Color Core, Glow;
        }

        private static readonly Def[] Defs =
        {
            new Def("seraphine", "SERAPHINE", true, 1.4f, 1.9f, new[] { "idle", "walk", "attack_thrust", "attack_throw" },
                new[] { BattleImpactKind.SwordSteel, BattleImpactKind.StormCrackle }, new Color(1f, 0.95f, 0.7f), new Color(0.45f, 0.7f, 1f)),
            new Def("kaelen", "KAELEN", true, -1.4f, 1.7f, new[] { "idle", "walk", "attack_dualslash" },
                new[] { BattleImpactKind.SwordSteel }, new Color(0.85f, 0.95f, 1f), new Color(0.5f, 0.8f, 1f)),
            new Def("vorgath", "VORGATH", false, -1.4f, 2.3f, new[] { "idle", "walk", "attack_slam", "attack_spit" },
                new[] { BattleImpactKind.GroundSlam, BattleImpactKind.ArcaneConjure }, new Color(0.85f, 0.55f, 1f), new Color(0.45f, 0.1f, 0.7f)),
            new Def("malzareth", "MALZARETH", false, 1.4f, 1.9f, new[] { "idle", "walk", "attack_greatsword" },
                new[] { BattleImpactKind.VoidFireBreath }, new Color(0.8f, 0.5f, 1f), new Color(0.3f, 0.05f, 0.5f)),
        };

        private sealed class Unit
        {
            public Def Def = null!;
            public Transform Host = null!;
            public GameObject Model = null!;
            public Transform? Hips;
            public Vector3 HipsRest;
            public readonly List<AnimationClip?> Clips = new List<AnimationClip?>();
            public int Clip;
            public float Time;
            public bool Manual;
            public bool HitDone;
            public int NextAttack;
            public float IdleLeft;
            public bool Arrived;
            public float Height = 1.8f;
            public bool Frozen = true;
            public Transform[] Bones = System.Array.Empty<Transform>();
            public Vector3[] FromPos = System.Array.Empty<Vector3>();
            public Quaternion[] FromRot = System.Array.Empty<Quaternion>();
            public float BlendLeft;
            public Unit? Target;
            public ClothSpringRig? Cloth;
            public Text? Now;
        }

        private readonly List<Unit> _units = new List<Unit>();
        private LockedBattleCamera? _camera;
        private bool _turned;
        private bool _orbit;
        private Text? _orbitLabel;

        private void Start()
        {
            var camGo = new GameObject("NewCharsPreviewCamera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = LockedBattleGround.Horizon;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;
            BuildLights();
            LockedBattleGround.Build(transform);
            foreach (var def in Defs)
            {
                var u = Spawn(def);
                if (u != null)
                {
                    _units.Add(u);
                }
            }

            foreach (var u in _units)
            {
                u.Target = Nearest(u);
            }

            _camera = camGo.AddComponent<LockedBattleCamera>();
            _camera.SetField(new Bounds(new Vector3(0f, 1f, 0f), new Vector3(8f, 2.5f, 5f)), false);
            // The menu saves an empty scene with only this component, so there is no EventSystem and the HUD buttons
            // never receive clicks. Create it at runtime so already-generated scenes are repaired too.
            SurvivalVisuals.EnsureEventSystem();
            BuildHud();
            Restart();
        }

        // ---------- loading ----------

        private Unit? Spawn(Def def)
        {
            var dir = Dir + def.Folder + "/";
            var prefab = PlayerAssets.Load<GameObject>(dir + def.Prefix + "_rig_rest.fbx");
            if (prefab == null)
            {
                Debug.LogError("New characters preview: missing " + dir + def.Prefix + "_rig_rest.fbx");
                return null;
            }

            var host = new GameObject(def.Prefix + "_Preview").transform;
            host.SetParent(transform, false);
            var model = Instantiate(prefab, host);
            model.name = def.Prefix;
            foreach (var a in model.GetComponentsInChildren<Animator>(true))
            {
                Destroy(a);
            }

            ApplyMaterial(model, PlayerAssets.Load<Texture2D>(dir + def.Prefix + "_atlas_2048.png"), def.Prefix + "_atlas", false);
            var u = new Unit { Def = def, Host = host, Model = model };
            foreach (var clipName in def.Clips)
            {
                u.Clips.Add(LoadClip(dir + def.Prefix + "_" + clipName + ".fbx"));
            }

            u.Hips = FindBone(model.transform, "mixamorig:Hips");
            if (u.Hips != null)
            {
                u.HipsRest = model.transform.InverseTransformPoint(u.Hips.position);
            }

            // Scale guard (FBX unit drift) and feet on the ground, measured in the bind pose.
            var b = BoundsOf(model);
            if (b.size.y > 0.01f && (b.size.y < 0.5f || b.size.y > 6f))
            {
                var s = 1.85f / b.size.y;
                model.transform.localScale *= s;
                Debug.LogWarning(def.Prefix + ": FBX height " + b.size.y.ToString("0.00") + " m. Rescaled by " + s.ToString("0.000") + ".");
                b = BoundsOf(model);
            }

            model.transform.position += Vector3.up * (host.position.y - b.min.y);
            u.Height = BoundsOf(model).size.y;
            if (u.Hips != null)
            {
                u.HipsRest = model.transform.InverseTransformPoint(u.Hips.position);
            }

            LockedBattleGround.AddContactShadow(host, 0.8f, 0.6f);
            // Body bones only (before the cape is attached): the crossfade snapshots these.
            var bones = new List<Transform>();
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t != model.transform)
                {
                    bones.Add(t);
                }
            }

            u.Bones = bones.ToArray();
            u.FromPos = new Vector3[u.Bones.Length];
            u.FromRot = new Quaternion[u.Bones.Length];
            AttachCape(u, dir);
            return u;
        }

        private static AnimationClip? LoadClip(string path)
        {
            foreach (var o in PlayerAssets.LoadAll(path))
            {
                if (o is AnimationClip c && !c.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                {
                    return c;
                }
            }

            Debug.LogWarning("New characters preview: no clip in " + path);
            return null;
        }

        private void AttachCape(Unit u, string dir)
        {
            var prefab = PlayerAssets.Load<GameObject>(dir + u.Def.Prefix + "_cape.fbx");
            if (prefab == null)
            {
                return; // Vorgath: no cape.
            }

            var spine2 = FindBone(u.Model.transform, "mixamorig:Spine2");
            var cape = Instantiate(prefab, u.Model.transform);
            cape.name = u.Def.Prefix + "_cape";
            cape.transform.localPosition = Vector3.zero;
            cape.transform.localRotation = Quaternion.identity;
            cape.transform.localScale = Vector3.one;
            foreach (var a in cape.GetComponentsInChildren<Animator>(true))
            {
                Destroy(a);
            }

            ApplyMaterial(cape, PlayerAssets.Load<Texture2D>(dir + u.Def.Prefix + "_cape_1024.png"), u.Def.Prefix + "_cape", true);
            var root = FindBone(cape.transform, "cape_root");
            if (root == null || spine2 == null)
            {
                Debug.LogError(u.Def.Prefix + " cape: cape_root or mixamorig:Spine2 missing.");
                return;
            }

            // CAPE_ATTACH.md: cape_root's bind matrix equals Spine2's, so an identity local under Spine2.
            root.SetParent(spine2, false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            foreach (var skin in cape.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.updateWhenOffscreen = true;
            }

            var spec = CapeSpec(u, root, PlayerAssets.FileExists(dir + "CAPE_ATTACH.md") ? PlayerAssets.ReadAllText(dir + "CAPE_ATTACH.md") : "");
            u.Cloth = u.Model.AddComponent<ClothSpringRig>();
            if (!u.Cloth.Bind(spec, u.Model.transform))
            {
                Debug.LogWarning(u.Def.Prefix + " cape springs did not bind fully: " + string.Join(", ", u.Cloth.MissingBones));
            }
        }

        /// <summary>3 chains (L/C/R) on cape_root, joints measured from the bound pose; colliders from the CAPE_ATTACH.md table.</summary>
        private static ClothSplitSpec CapeSpec(Unit u, Transform root, string notes)
        {
            var model = u.Model.transform;
            var chains = new List<ClothChainSpec>();
            foreach (var side in new[] { "L", "C", "R" })
            {
                var names = new[] { "cape_" + side + "_01", "cape_" + side + "_02", "cape_" + side + "_03" };
                var joints = new float[12];
                var pts = new Vector3[4];
                var ok = true;
                for (var i = 0; i < 3; i++)
                {
                    var t = FindBone(root, names[i]);
                    if (t == null)
                    {
                        ok = false;
                        break;
                    }

                    pts[i] = model.InverseTransformPoint(t.position);
                }

                if (!ok)
                {
                    continue;
                }

                pts[3] = pts[2] + (pts[2] - pts[1]);
                for (var i = 0; i < 4; i++)
                {
                    joints[i * 3] = pts[i].x;
                    joints[(i * 3) + 1] = pts[i].y;
                    joints[(i * 3) + 2] = pts[i].z;
                }

                var length = Vector3.Distance(pts[0], pts[1]) + Vector3.Distance(pts[1], pts[2]) + Vector3.Distance(pts[2], pts[3]);
                // Design preview: stiffness 1.0, drag 0.4, gravity 0.4 (VRM style); mapped onto this solver's ranges.
                chains.Add(new ClothChainSpec("cape_" + side, side, names, "cape_root", length, 0, 0.35f, 0.4f, 0.4f, 0.4f, 0.0f, 40f, joints));
            }

            return new ClothSplitSpec(u.Def.Prefix, Dir + u.Def.Folder, 0, 0f, "", "", "", "", "", "", "", "", chains.ToArray(), Colliders(notes));
        }

        /// <summary>Rows "| mixamorig:Bone | radius | joint hit |": a capsule head→child bone, joint hit radius folded into the radius.</summary>
        private static ClothColliderSpec[] Colliders(string notes)
        {
            var tails = new Dictionary<string, string>
            {
                { "mixamorig:Hips", "mixamorig:Spine" }, { "mixamorig:Spine", "mixamorig:Spine1" }, { "mixamorig:Spine1", "mixamorig:Spine2" },
                { "mixamorig:Spine2", "mixamorig:Neck" }, { "mixamorig:LeftUpLeg", "mixamorig:LeftLeg" }, { "mixamorig:RightUpLeg", "mixamorig:RightLeg" },
                { "mixamorig:LeftLeg", "mixamorig:LeftFoot" }, { "mixamorig:RightLeg", "mixamorig:RightFoot" },
                { "mixamorig:LeftFoot", "mixamorig:LeftToeBase" }, { "mixamorig:RightFoot", "mixamorig:RightToeBase" },
                // v4: arm capsules (Kaelen) keep the cape from sinking into the body while the arms swing.
                { "mixamorig:LeftArm", "mixamorig:LeftForeArm" }, { "mixamorig:RightArm", "mixamorig:RightForeArm" },
                { "mixamorig:LeftForeArm", "mixamorig:LeftHand" }, { "mixamorig:RightForeArm", "mixamorig:RightHand" },
            };
            var list = new List<ClothColliderSpec>();
            foreach (var raw in notes.Split('\n'))
            {
                var cells = raw.Trim().Trim('|').Split('|');
                if (cells.Length < 3)
                {
                    continue;
                }

                var bone = cells[0].Trim();
                if (!tails.TryGetValue(bone, out var tail) ||
                    !float.TryParse(cells[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var r) ||
                    !float.TryParse(cells[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var hit))
                {
                    continue;
                }

                list.Add(new ClothColliderSpec(bone.Substring("mixamorig:".Length), ClothColliderKind.Capsule, bone, tail, r + hit));
            }

            if (list.Count == 0)
            {
                list.AddRange(ClothSplitSpec.MixamoLegColliders(0.18f, 0.16f, 0.12f));
            }

            return list.ToArray();
        }

        private static void ApplyMaterial(GameObject go, Texture2D? tex, string name, bool twoSided)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                return;
            }

            var mat = new Material(shader) { name = name };
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTexture("_MainTex", tex);
            }
            else
            {
                Debug.LogError("New characters preview: texture missing for " + name);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
            if (twoSided && mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            foreach (var rend in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[Mathf.Max(1, rend.sharedMaterials.Length)];
                for (var i = 0; i < mats.Length; i++)
                {
                    mats[i] = mat;
                }

                rend.sharedMaterials = mats;
                if (rend is SkinnedMeshRenderer skin)
                {
                    skin.updateWhenOffscreen = true;
                }
            }
        }

        // ---------- play ----------

        private void Restart()
        {
            if (_orbit)
            {
                ToggleOrbit();
            }

            foreach (var u in _units)
            {
                var sign = u.Def.Hero ? -1f : 1f;
                u.Host.position = new Vector3(sign * u.Def.StopX, 0f, u.Def.Z);
                u.Arrived = true;
                u.Manual = true;
                u.Frozen = true;
                u.BlendLeft = 0f;
                u.NextAttack = 0;
                Play(u, 0);
                u.BlendLeft = 0f;
                if (u.Now != null) u.Now.text = "STILL";
                Face(u);
                var idle = u.Clips[0];
                if (idle != null)
                {
                    idle.SampleAnimation(u.Model, 0f);
                    HoldHips(u);
                }

                u.Cloth?.ResetToRest();
            }

            _camera?.ResetView();
        }

        private void Play(Unit u, int clip)
        {
            Snapshot(u);
            u.Clip = Mathf.Clamp(clip, 0, u.Clips.Count - 1);
            u.Time = 0f;
            u.HitDone = false;
            if (u.Now != null)
            {
                u.Now.text = u.Def.Clips[u.Clip].Replace("attack_", "").ToUpperInvariant();
            }
        }

        private static bool Loops(Unit u) => u.Clip <= 1;

        /// <summary>Captures the current pose so the next clip fades in from it (no snap).</summary>
        private static void Snapshot(Unit u)
        {
            for (var i = 0; i < u.Bones.Length; i++)
            {
                u.FromPos[i] = u.Bones[i].localPosition;
                u.FromRot[i] = u.Bones[i].localRotation;
            }

            u.BlendLeft = Blend;
        }

        private static void HoldHips(Unit u)
        {
            if (u.Hips == null)
            {
                return;
            }

            var local = u.Model.transform.InverseTransformPoint(u.Hips.position);
            local.x = u.HipsRest.x;
            local.z = u.HipsRest.z;
            u.Hips.position = u.Model.transform.TransformPoint(local);
        }

        private void Update()
        {
            Keys();
            var dt = Time.deltaTime;
            foreach (var u in _units)
            {
                Tick(u, dt);
            }
        }

        private void Tick(Unit u, float dt)
        {
            var clip = u.Clips[u.Clip];
            var length = clip != null ? Mathf.Max(0.1f, clip.length) : 1f;
            Face(u);
            if (u.Frozen)
            {
                return; // Pose was set on Restart; nothing moves until a button.
            }

            u.Time += dt;
            if (!u.Manual)
            {
                // Opt-in auto: idle a moment, then one attack, back to idle.
                if (u.Clip == 0)
                {
                    u.IdleLeft -= dt;
                    if (u.IdleLeft <= 0f && u.Clips.Count > 2)
                    {
                        Play(u, 2 + (u.NextAttack++ % (u.Clips.Count - 2)));
                    }
                }
                else if (u.Clip >= 2 && u.Time >= length)
                {
                    Play(u, 0);
                    u.IdleLeft = IdleGap;
                }
            }
            else if (!Loops(u) && u.Time >= length)
            {
                // Manual attack loops until the next button; blend the wrap so the end pose doesn't pop to the start.
                Snapshot(u);
                u.Time -= length;
                u.HitDone = false;
            }

            if (u.Clip >= 2 && !u.HitDone && u.Time >= length * HitAt)
            {
                u.HitDone = true;
                Hit(u);
            }

            if (clip != null)
            {
                var t = Loops(u) ? Mathf.Repeat(u.Time, length) : Mathf.Min(u.Time, length);
                clip.SampleAnimation(u.Model, t);
            }

            // In place for every clip: attacks too, so a looping attack never slides off and snaps back.
            HoldHips(u);

            if (u.BlendLeft > 0f)
            {
                u.BlendLeft = Mathf.Max(0f, u.BlendLeft - dt);
                var w = 1f - (u.BlendLeft / Blend);
                w = w * w * (3f - (2f * w));
                for (var i = 0; i < u.Bones.Length; i++)
                {
                    var bone = u.Bones[i];
                    bone.localPosition = Vector3.Lerp(u.FromPos[i], bone.localPosition, w);
                    bone.localRotation = Quaternion.Slerp(u.FromRot[i], bone.localRotation, w);
                }
            }
        }

        private void Face(Unit u)
        {
            var dir = u.Def.Hero ? Vector3.right : Vector3.left;
            if (u.Target != null && u.Arrived)
            {
                dir = u.Target.Host.position - u.Host.position;
                dir.y = 0f;
            }

            if (_turned)
            {
                dir = -dir;
            }

            if (dir.sqrMagnitude > 1e-6f)
            {
                u.Host.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        private void Hit(Unit u)
        {
            var target = u.Target;
            if (target == null)
            {
                return;
            }

            var impact = u.Def.Impacts[Mathf.Clamp(u.Clip - 2, 0, u.Def.Impacts.Length - 1)];
            var point = target.Host.position + (Vector3.up * target.Height * (impact == BattleImpactKind.GroundSlam ? 0.05f : 0.55f));
            LockedBattleImpact.Spawn(impact, point, target.Host.position - u.Host.position, u.Def.Core, u.Def.Glow, Color.white, 1f);
        }

        private Unit? Nearest(Unit u)
        {
            Unit? best = null;
            var bestD = float.MaxValue;
            foreach (var o in _units)
            {
                if (o.Def.Hero == u.Def.Hero)
                {
                    continue;
                }

                var d = Mathf.Abs(o.Def.Z - u.Def.Z);
                if (d < bestD)
                {
                    bestD = d;
                    best = o;
                }
            }

            return best;
        }

        // ---------- controls ----------

        private void Keys()
        {
            var kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            for (var i = 0; i < _units.Count && i < 4; i++)
            {
                var key = i == 0 ? kb.digit1Key : i == 1 ? kb.digit2Key : i == 2 ? kb.digit3Key : kb[Key.Digit4];
                if (key.wasPressedThisFrame)
                {
                    Tap(_units[i], (_units[i].Clip + 1) % _units[i].Clips.Count);
                }
            }

            if (kb.tKey.wasPressedThisFrame) _turned = !_turned;
            if (kb.qKey.wasPressedThisFrame) ToggleOrbit();
            if (kb.aKey.wasPressedThisFrame) Auto();
            if (kb.rKey.wasPressedThisFrame) Restart();
        }

        private void Tap(Unit u, int clip)
        {
            u.Manual = true;
            u.Arrived = true;
            u.Frozen = false;
            Play(u, clip);
        }

        private void Auto()
        {
            foreach (var u in _units)
            {
                if (u.Manual)
                {
                    u.Manual = false;
                    u.Frozen = false;
                    Play(u, 0);
                    u.IdleLeft = IdleGap;
                }
            }
        }

        private void ToggleOrbit()
        {
            _orbit = !_orbit;
            _camera?.SetField(new Bounds(new Vector3(0f, 1f, 0f), new Vector3(8f, 2.5f, 5f)), _orbit);
            if (_orbitLabel != null)
            {
                _orbitLabel.text = _orbit ? "ORBIT ON" : "ORBIT OFF";
            }
        }

        private void BuildHud()
        {
            var canvas = SurvivalVisuals.Canvas(transform, "NewCharsPreviewHud", 80);
            var title = HookSkin.Outlined(SurvivalVisuals.Text(canvas, "Title", "NEW CHARACTERS PREVIEW", 30, TextAnchor.MiddleCenter, HookSkin.Cream));
            HookSkin.Box(title.rectTransform, 0.05f, 0.95f, 0.95f, 0.99f);

            TopButton(canvas, "TURN AROUND", 0.02f, () => _turned = !_turned);
            _orbitLabel = TopButton(canvas, "ORBIT OFF", 0.26f, ToggleOrbit);
            TopButton(canvas, "AUTO", 0.50f, Auto);
            TopButton(canvas, "RESTART", 0.74f, Restart);

            for (var i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                var x0 = 0.01f + (i * 0.2475f);
                var x1 = x0 + 0.2375f;
                var name = HookSkin.Outlined(SurvivalVisuals.Text(canvas, u.Def.Prefix + "Name", (i + 1) + "  " + u.Def.Prefix, 22, TextAnchor.MiddleCenter, HookSkin.Cream));
                HookSkin.Box(name.rectTransform, x0, 0.265f, x1, 0.30f);
                u.Now = HookSkin.Outlined(SurvivalVisuals.Text(canvas, u.Def.Prefix + "Now", "STILL", 18, TextAnchor.MiddleCenter, HookSkin.Cream));
                HookSkin.Box(u.Now.rectTransform, x0, 0.235f, x1, 0.265f);
                for (var c = 0; c < u.Clips.Count; c++)
                {
                    var clip = c;
                    var y1 = 0.23f - (c * 0.055f);
                    var label = u.Def.Clips[c].Replace("attack_", "").ToUpperInvariant() + (u.Clips[c] == null ? " (missing)" : "");
                    var key = c >= 2 ? HookArtKeys.ButtonRed : HookArtKeys.ButtonBlue;
                    var btn = HookSkin.Button(canvas, u.Def.Prefix + "_" + label, key, new Color(0.2f, 0.25f, 0.4f, 0.9f));
                    HookSkin.Box(btn.GetComponent<RectTransform>(), x0, y1 - 0.05f, x1, y1);
                    var text = HookSkin.Outlined(SurvivalVisuals.Text(btn.transform, "Label", label, 20, TextAnchor.MiddleCenter, HookSkin.Cream));
                    SurvivalVisuals.Stretch(text.rectTransform);
                    btn.onClick.AddListener(() => Tap(u, clip));
                }
            }
        }

        private static Text TopButton(RectTransform canvas, string label, float x0, UnityEngine.Events.UnityAction onClick)
        {
            var btn = HookSkin.Button(canvas, label, HookArtKeys.ButtonGold, new Color(0.35f, 0.28f, 0.12f, 0.9f));
            HookSkin.Box(btn.GetComponent<RectTransform>(), x0, 0.885f, x0 + 0.22f, 0.94f);
            var text = HookSkin.Outlined(SurvivalVisuals.Text(btn.transform, "Label", label, 20, TextAnchor.MiddleCenter, HookSkin.Cream));
            SurvivalVisuals.Stretch(text.rectTransform);
            btn.onClick.AddListener(onClick);
            return text;
        }

        // ---------- helpers ----------

        private static void BuildLights()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Rgb(BattleLight.SkyAmbient);
            RenderSettings.ambientEquatorColor = Rgb(BattleLight.EquatorAmbient);
            RenderSettings.ambientGroundColor = Rgb(BattleLight.GroundAmbient);
            var sun = new GameObject("NewCharsPreviewSun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = BattleLight.SunIntensity;
            sun.color = Rgb(BattleLight.SunColour);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(BattleLight.SunPitchDegrees, BattleLight.SunYawDegrees(LockedBattleCamera.DefaultYawDegrees), 0f);
        }

        private static Color Rgb((float R, float G, float B) c) => new Color(c.R, c.G, c.B, 1f);

        private static Transform? FindBone(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0)
            {
                return new Bounds(go.transform.position, Vector3.one);
            }

            var b = rs[0].bounds;
            foreach (var r in rs)
            {
                b.Encapsulate(r.bounds);
            }

            return b;
        }
    }
}
