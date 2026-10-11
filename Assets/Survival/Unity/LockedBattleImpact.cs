using System.Collections.Generic;
using Survival.Domain.Roster;
using UnityEngine;
using UnityEngine.Rendering;

namespace Survival.Unity
{
    /// <summary>
    /// One short impact burst (0.3–0.5 s) where a locked battle attack lands. Built from the same
    /// pieces as <see cref="BlenderRigAttackDriver"/> effects: URP Particles/Unlit with the soft
    /// dot, additive glow, view-aligned lines, and the same fade gradient. Dark smoke and dust use
    /// an alpha-blended copy of that material. Destroys itself when done.
    /// </summary>
    public sealed class LockedBattleImpact : MonoBehaviour
    {
        private static Material? s_additive;
        private static Material? s_alpha;
        private static Texture2D? s_dot;

        private readonly List<LineRenderer> _arcs = new List<LineRenderer>();
        private readonly List<Streak> _streaks = new List<Streak>();
        private Light? _light;
        private float _lightPeak;
        private float _lightSeconds;
        private float _arcSeconds;
        private float _arcLength;
        private float _life = 0.6f;
        private float _age;
        private int _arcStep = -1;

        /// <summary>Spawns the burst for <paramref name="kind"/> at <paramref name="point"/>. Travel is attacker → defender.</summary>
        public static LockedBattleImpact Spawn(BattleImpactKind kind, Vector3 point, Vector3 travel, Color core, Color glow, Color edge)
        {
            travel.y = 0f;
            if (travel.sqrMagnitude < 1e-8f)
            {
                travel = Vector3.forward;
            }

            travel.Normalize();
            var go = new GameObject("LockedBattleImpact_" + kind);
            go.transform.SetPositionAndRotation(point, Quaternion.LookRotation(-travel, Vector3.up));
            var fx = go.AddComponent<LockedBattleImpact>();
            switch (kind)
            {
                case BattleImpactKind.FireBurst:
                    fx.BuildFireBurst(core, glow, edge);
                    break;
                case BattleImpactKind.StormCrackle:
                    fx.BuildStormCrackle(core, glow, edge);
                    break;
                case BattleImpactKind.ShadowRake:
                    fx.BuildShadowRake(core, glow, edge, travel);
                    break;
                default:
                    fx.BuildIronClaw(core, glow, edge, travel);
                    break;
            }

            return fx;
        }

        /// <summary>Fireball hit: white-gold flash, rolling flame puff, falling embers, sparks, orange scorch light.</summary>
        private void BuildFireBurst(Color core, Color glow, Color edge)
        {
            _life = 0.5f;
            Burst("Flash", Additive(), 1, 0.12f, 0.12f, 0f, 0f, 0.55f, 0.65f, Bright(core), core, glow, 0f, 0.02f, 180f, 0.25f);
            Burst("Flame", Additive(), 26, 0.22f, 0.40f, 1.0f, 2.6f, 0.14f, 0.30f, core, glow, edge, -0.25f, 0.10f, 70f, 1.6f);
            Burst("Embers", Additive(), 18, 0.35f, 0.60f, 2.4f, 4.4f, 0.02f, 0.05f, glow, edge, edge, 0.9f, 0.05f, 60f, 0.4f);
            Burst("Sparks", Additive(), 12, 0.12f, 0.22f, 4.0f, 6.0f, 0.015f, 0.03f, Bright(core), glow, edge, 0.4f, 0.04f, 45f, 0.3f);
            AddLight(new Color(1f, 0.55f, 0.18f, 1f), 7f, 2.6f, 0.18f);
        }

        /// <summary>Storm bolt hit: blue-white flash, crackling arcs that re-fork every other frame, arcing sparks, blue light.</summary>
        private void BuildStormCrackle(Color core, Color glow, Color edge)
        {
            _life = 0.38f;
            Burst("Flash", Additive(), 1, 0.08f, 0.08f, 0f, 0f, 0.70f, 0.80f, Bright(core), core, glow, 0f, 0.02f, 180f, 0.3f);
            Burst("Sparks", Additive(), 30, 0.20f, 0.40f, 3.0f, 6.0f, 0.015f, 0.04f, Bright(core), glow, edge, 1.2f, 0.06f, 80f, 0.3f);
            Burst("Glow", Additive(), 6, 0.15f, 0.25f, 0.2f, 0.6f, 0.25f, 0.40f, glow, edge, edge, 0f, 0.08f, 180f, 1.4f);
            _arcSeconds = 0.25f;
            _arcLength = 0.55f;
            for (var i = 0; i < 4; i++)
            {
                _arcs.Add(Line("Arc" + i, Additive(), 7, i % 2 == 0 ? 0.035f : 0.018f, Bright(core), glow));
            }

            AddLight(new Color(0.55f, 0.75f, 1f, 1f), 9f, 3.2f, 0.12f);
        }

        /// <summary>Claw rake hit: three violet claw streaks that rip across the hit, dark wisps, violet sparks.</summary>
        private void BuildShadowRake(Color core, Color glow, Color edge, Vector3 travel)
        {
            _life = 0.45f;
            var dark = new Color(0.10f, 0.03f, 0.16f, 0.85f);
            var darker = new Color(0.04f, 0.01f, 0.07f, 0.6f);
            Burst("Wisps", Alpha(), 10, 0.35f, 0.55f, 0.3f, 0.7f, 0.18f, 0.36f, dark, darker, darker, -0.15f, 0.12f, 120f, 2.0f);
            Burst("Sparks", Additive(), 14, 0.15f, 0.30f, 2.0f, 4.0f, 0.015f, 0.035f, Bright(core), glow, edge, 0.6f, 0.05f, 70f, 0.3f);
            AddStreaks(core, glow, travel, 0.36f, 0.05f, 0.11f, 0.42f, 40f);
            AddLight(new Color(0.55f, 0.25f, 0.85f, 1f), 3f, 2.0f, 0.15f);
        }

        /// <summary>Ironhowl claw hit: hot claw sparks, three short white slash streaks, a dust puff, falling debris.</summary>
        private void BuildIronClaw(Color core, Color glow, Color edge, Vector3 travel)
        {
            _life = 0.55f;
            var dust = new Color(0.46f, 0.41f, 0.34f, 0.55f);
            var dustEnd = new Color(0.38f, 0.34f, 0.29f, 0.25f);
            var grit = new Color(0.20f, 0.18f, 0.15f, 1f);
            Burst("Sparks", Additive(), 26, 0.18f, 0.35f, 3.0f, 6.0f, 0.015f, 0.04f, Bright(core), glow, edge, 1.5f, 0.05f, 60f, 0.3f);
            Burst("Dust", Alpha(), 8, 0.40f, 0.60f, 0.4f, 0.9f, 0.25f, 0.50f, dust, dustEnd, dustEnd, -0.05f, 0.15f, 120f, 1.8f);
            Burst("Debris", Alpha(), 10, 0.40f, 0.60f, 1.5f, 3.0f, 0.03f, 0.06f, grit, grit, grit, 2.5f, 0.08f, 70f, 1f);
            AddStreaks(core, glow, travel, 0.30f, 0.04f, 0.07f, 0.30f, -55f);
            AddLight(new Color(1f, 0.70f, 0.40f, 1f), 4f, 2.0f, 0.10f);
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            if (_light != null)
            {
                var k = _lightSeconds > 0f ? Mathf.Clamp01(1f - (_age / _lightSeconds)) : 0f;
                _light.intensity = _lightPeak * k * k;
                _light.enabled = k > 0f;
            }

            if (_arcs.Count > 0)
            {
                var on = _age < _arcSeconds;
                var step = Mathf.FloorToInt(_age * 30f / 2f);
                foreach (var arc in _arcs)
                {
                    arc.enabled = on;
                }

                if (on && step != _arcStep)
                {
                    _arcStep = step;
                    RollArcs(1f - (_age / _arcSeconds));
                }
            }

            foreach (var streak in _streaks)
            {
                streak.Tick(_age);
            }

            if (_age >= _life)
            {
                Destroy(gameObject);
            }
        }

        private void RollArcs(float strength)
        {
            var origin = transform.position;
            foreach (var arc in _arcs)
            {
                var dir = Random.onUnitSphere;
                dir += transform.forward * 0.6f;
                dir.Normalize();
                var length = _arcLength * Random.Range(0.6f, 1f) * Mathf.Max(0.35f, strength);
                var n = arc.positionCount;
                for (var i = 0; i < n; i++)
                {
                    var t = i / (float)(n - 1);
                    var j = i == 0 ? 0f : length * 0.12f;
                    arc.SetPosition(i, origin + (dir * (t * length)) + (Random.insideUnitSphere * j));
                }
            }
        }

        private void AddStreaks(Color core, Color glow, Vector3 travel, float length, float spacing, float sweepSeconds, float lifeSeconds, float tiltDegrees)
        {
            var right = Vector3.Cross(Vector3.up, travel).normalized;
            var across = (Quaternion.AngleAxis(tiltDegrees, travel) * right).normalized;
            var stack = Vector3.Cross(across, travel).normalized;
            var origin = transform.position - (travel * 0.04f);
            for (var i = 0; i < 3; i++)
            {
                var offset = stack * ((i - 1) * spacing);
                var start = origin + offset - (across * (length * 0.5f));
                var end = origin + offset + (across * (length * 0.5f));
                var line = Line("Streak" + i, Additive(), 2, 0.035f, Bright(core), glow);
                line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.1f));
                var streak = new Streak(line, start, end, sweepSeconds + (i * 0.02f), lifeSeconds);
                streak.Tick(0f);
                _streaks.Add(streak);
            }
        }

        private void AddLight(Color color, float intensity, float range, float seconds)
        {
            var go = new GameObject("Flash");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            _light = light;
            _lightPeak = intensity;
            _lightSeconds = seconds;
        }

        /// <summary>
        /// One-shot particle burst. World space, so the burst stays where the hit landed.
        /// Cone opens back toward the attacker (this object's +Z); 180° is a full sphere.
        /// </summary>
        private void Burst(
            string name,
            Material material,
            int count,
            float lifeMin,
            float lifeMax,
            float speedMin,
            float speedMax,
            float sizeMin,
            float sizeMax,
            Color a,
            Color b,
            Color c,
            float gravity,
            float radius,
            float coneDegrees,
            float sizeEnd)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = Color.white;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = Mathf.Max(count, 1);
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.enabled = true;
            if (coneDegrees >= 179f)
            {
                shape.shapeType = ParticleSystemShapeType.Sphere;
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = Mathf.Clamp(coneDegrees, 0f, 89f);
            }

            shape.radius = Mathf.Max(radius, 0.0001f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Fade(a, b, c));
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, sizeEnd));
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = material;
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            ps.Play(true);
        }

        private LineRenderer Line(string name, Material material, int count, float width, Color a, Color b)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = count;
            line.widthMultiplier = width;
            line.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.35f);
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.75f, 1f) });
            line.colorGradient = g;
            line.sharedMaterial = material;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (var i = 0; i < count; i++)
            {
                line.SetPosition(i, transform.position);
            }

            return line;
        }

        private static Color Bright(Color c)
        {
            return Color.Lerp(c, Color.white, 0.5f);
        }

        private static Gradient Fade(Color a, Color b, Color c)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.45f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(a.a, 0f), new GradientAlphaKey(b.a * 0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private static Material Additive()
        {
            if (s_additive == null)
            {
                s_additive = MakeParticleMaterial("LockedBattleImpactAdditive", additive: true);
            }

            return s_additive;
        }

        private static Material Alpha()
        {
            if (s_alpha == null)
            {
                s_alpha = MakeParticleMaterial("LockedBattleImpactAlpha", additive: false);
            }

            return s_alpha;
        }

        /// <summary>Same recipe as the attack driver's additive effect material, with an alpha-blend option.</summary>
        private static Material MakeParticleMaterial(string name, bool additive)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.name = name;
            if (s_dot == null)
            {
                s_dot = SoftDot();
            }

            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", s_dot);
            }

            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", s_dot);
            }

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", Color.white);
            }

            SetFloat(mat, "_Surface", 1f);
            SetFloat(mat, "_Blend", additive ? 2f : 0f);
            SetFloat(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloat(mat, "_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            SetFloat(mat, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloat(mat, "_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            SetFloat(mat, "_ZWrite", 0f);
            SetFloat(mat, "_Cull", (float)CullMode.Off);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }

        private static void SetFloat(Material mat, string name, float value)
        {
            if (mat.HasProperty(name))
            {
                mat.SetFloat(name, value);
            }
        }

        private static Texture2D SoftDot()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.name = "LockedBattleImpactDot";
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[n * n];
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var dx = ((x + 0.5f) / n * 2f) - 1f;
                    var dy = ((y + 0.5f) / n * 2f) - 1f;
                    var r = Mathf.Clamp01(Mathf.Sqrt((dx * dx) + (dy * dy)));
                    var a = (1f - r) * (1f - r);
                    pixels[(y * n) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>A claw streak that rips from start to end, then thins out and fades.</summary>
        private sealed class Streak
        {
            private readonly Vector3 _start;
            private readonly Vector3 _end;
            private readonly float _sweep;
            private readonly float _life;
            private readonly float _width;

            public Streak(LineRenderer line, Vector3 start, Vector3 end, float sweep, float life)
            {
                Line = line;
                _start = start;
                _end = end;
                _sweep = Mathf.Max(sweep, 0.01f);
                _life = Mathf.Max(life, _sweep + 0.01f);
                _width = line.widthMultiplier;
            }

            public LineRenderer Line { get; }

            public void Tick(float age)
            {
                if (Line == null)
                {
                    return;
                }

                var grow = Mathf.Clamp01(age / _sweep);
                var fade = age <= _sweep ? 1f : Mathf.Clamp01(1f - ((age - _sweep) / (_life - _sweep)));
                var tail = age <= _sweep ? 0f : (1f - fade) * 0.6f;
                Line.SetPosition(0, Vector3.Lerp(_start, _end, tail));
                Line.SetPosition(1, Vector3.Lerp(_start, _end, Mathf.Max(grow, 0.02f)));
                Line.widthMultiplier = _width * fade;
                Line.enabled = fade > 0.01f;
            }
        }
    }
}
