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
        private LineRenderer? _flightLine;
        private Vector3 _flightFrom;
        private Vector3 _flightTo;
        private float _flightLoft;
        private float _flightSeconds;

        /// <summary>Burst size multiplier: the attacker's battle scale (2 for the dragons), 1 for everyone else.</summary>
        private float _size = 1f;

        /// <summary>
        /// An arrow's flight streak from an archer's extra clip (attacks 2 and 3): a short glowing dash that travels from the
        /// bow hand to the contact over <paramref name="seconds"/>, on a parabola <paramref name="loft"/> metres above the line
        /// (a volley). The hit burst itself is spawned when it lands, by the battle.
        /// </summary>
        public static LockedBattleImpact SpawnFlight(Vector3 from, Vector3 to, float seconds, float loft, Color core, Color glow)
        {
            var go = new GameObject("LockedBattleShotFlight");
            go.transform.position = from;
            var fx = go.AddComponent<LockedBattleImpact>();
            fx._flightFrom = from;
            fx._flightTo = to;
            fx._flightLoft = loft;
            fx._flightSeconds = Mathf.Max(seconds, 0.02f);
            fx._life = fx._flightSeconds + 0.04f;
            var line = fx.Line("Flight", Additive(), 6, 0.03f, glow, Bright(core));
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(1f, 1f));
            fx._flightLine = line;
            fx.TickFlight();
            return fx;
        }

        private Vector3 FlightPoint(float t)
        {
            t = Mathf.Clamp01(t);
            return Vector3.Lerp(_flightFrom, _flightTo, t) + (Vector3.up * (_flightLoft * 4f * t * (1f - t)));
        }

        private void TickFlight()
        {
            if (_flightLine == null)
            {
                return;
            }

            var head = _age / _flightSeconds;
            var tail = head - 0.3f;
            var n = _flightLine.positionCount;
            for (var i = 0; i < n; i++)
            {
                _flightLine.SetPosition(i, FlightPoint(Mathf.Lerp(tail, head, i / (float)(n - 1))));
            }

            _flightLine.enabled = head <= 1.05f;
        }

        /// <summary>Spawns the burst for <paramref name="kind"/> at <paramref name="point"/>. Travel is attacker → defender.</summary>
        public static LockedBattleImpact Spawn(BattleImpactKind kind, Vector3 point, Vector3 travel, Color core, Color glow, Color edge, float size = 1f)
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
            fx._size = size > 1e-3f ? size : 1f;
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
                case BattleImpactKind.VoidFireBreath:
                    fx.BuildVoidFireBreath(core, glow, edge, travel);
                    break;
                case BattleImpactKind.ArcaneConjure:
                    fx.BuildArcaneConjure(core, glow, edge);
                    break;
                case BattleImpactKind.StaffLight:
                    fx.BuildStaffLight(core, glow, edge);
                    break;
                case BattleImpactKind.LeafEarth:
                    fx.BuildLeafEarth(core, glow, edge, travel);
                    break;
                case BattleImpactKind.SwordSteel:
                    fx.BuildSwordSteel(core, glow, edge, travel);
                    break;
                case BattleImpactKind.BlightRot:
                    fx.BuildBlightRot(core, glow, edge, travel);
                    break;
                case BattleImpactKind.RangerArrow:
                    fx.BuildRangerArrow(core, glow, edge, travel);
                    break;
                case BattleImpactKind.BoneShard:
                    fx.BuildBoneShard(core, glow, edge, travel);
                    break;
                case BattleImpactKind.HeroStaffSweep:
                    fx.BuildHeroStaffSweep(travel);
                    break;
                case BattleImpactKind.HeroArcaneNova:
                    fx.BuildHeroArcaneNova();
                    break;
                case BattleImpactKind.GroundSlam:
                    fx.BuildGroundSlam(core, glow, edge, travel);
                    break;
                case BattleImpactKind.HeroRootSnare:
                    fx.BuildHeroRootSnare(travel);
                    break;
                case BattleImpactKind.VillainShadowLash:
                    fx.BuildVillainShadowLash(travel);
                    break;
                case BattleImpactKind.VillainHexRain:
                    fx.BuildVillainHexRain(travel);
                    break;
                case BattleImpactKind.HeroShieldShock:
                    fx.BuildHeroShieldShock(travel);
                    break;
                case BattleImpactKind.WingGust:
                    fx.BuildWingGust(core, glow, edge, travel);
                    break;
                case BattleImpactKind.StompQuake:
                    fx.BuildStompQuake(core, glow, edge, travel);
                    break;
                case BattleImpactKind.ClawWave:
                    fx.BuildClawWave(core, glow, edge, travel);
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

        /// <summary>Sir Aldric sword hit: white steel flash, two blue (#1b3060) slash streaks across the thrust, steel sparks, cool light.</summary>
        private void BuildSwordSteel(Color core, Color glow, Color edge, Vector3 travel)
        {
            _life = 0.42f;
            Burst("Flash", Additive(), 1, 0.08f, 0.08f, 0f, 0f, 0.45f, 0.55f, Bright(core), core, glow, 0f, 0.02f, 180f, 0.25f);
            Burst("Sparks", Additive(), 24, 0.15f, 0.30f, 3.5f, 6.5f, 0.012f, 0.03f, Bright(core), glow, edge, 1.4f, 0.04f, 55f, 0.25f);
            Burst("Glint", Additive(), 4, 0.10f, 0.18f, 0.1f, 0.4f, 0.18f, 0.28f, glow, edge, edge, 0f, 0.05f, 180f, 1.2f);
            AddStreaks(core, glow, travel, 0.40f, 0.06f, 0.07f, 0.28f, 30f);
            AddLight(new Color(0.62f, 0.76f, 1f, 1f), 5f, 2.2f, 0.10f);
        }

        /// <summary>Blightroot claw hit: purple rot flash, three claw streaks, a sickly spore puff and falling bark splinters.</summary>
        private void BuildBlightRot(Color core, Color glow, Color edge, Vector3 travel)
        {
            _life = 0.55f;
            var spore = new Color(0.30f, 0.12f, 0.36f, 0.70f);
            var sporeEnd = new Color(0.18f, 0.08f, 0.20f, 0.25f);
            var bark = new Color(0.22f, 0.15f, 0.12f, 1f);
            Burst("Flash", Additive(), 1, 0.10f, 0.10f, 0f, 0f, 0.50f, 0.60f, Bright(core), core, glow, 0f, 0.02f, 180f, 0.25f);
            Burst("Spores", Alpha(), 10, 0.40f, 0.60f, 0.3f, 0.8f, 0.20f, 0.42f, spore, sporeEnd, sporeEnd, -0.12f, 0.14f, 120f, 2.0f);
            Burst("Motes", Additive(), 16, 0.30f, 0.55f, 1.2f, 2.6f, 0.02f, 0.05f, glow, edge, edge, -0.3f, 0.06f, 80f, 0.4f);
            Burst("Splinters", Alpha(), 10, 0.40f, 0.60f, 1.5f, 3.0f, 0.025f, 0.05f, bark, bark, bark, 2.5f, 0.08f, 70f, 1f);
            AddStreaks(core, glow, travel, 0.38f, 0.06f, 0.10f, 0.40f, -40f);
            AddLight(new Color(0.65f, 0.30f, 0.90f, 1f), 4f, 2.2f, 0.14f);
        }

        /// <summary>Rowan arrow hit: hero-blue flash, one streak along the shot, wood splinters and green leaf flecks.</summary>
        private void BuildRangerArrow(Color core, Color glow, Color edge, Vector3 travel)
        {
            _life = 0.45f;
            var wood = new Color(0.45f, 0.32f, 0.18f, 1f);
            var leaf = new Color(0.32f, 0.60f, 0.24f, 1f);
            Burst("Flash", Additive(), 1, 0.08f, 0.08f, 0f, 0f, 0.40f, 0.50f, Bright(core), core, glow, 0f, 0.02f, 180f, 0.25f);
            Burst("Sparks", Additive(), 14, 0.15f, 0.28f, 2.5f, 5.0f, 0.012f, 0.03f, Bright(core), glow, edge, 0.8f, 0.04f, 50f, 0.25f);
            Burst("Splinters", Alpha(), 10, 0.35f, 0.55f, 1.5f, 3.0f, 0.02f, 0.045f, wood, wood, wood, 2.5f, 0.06f, 70f, 1f);
            Burst("Leaves", Alpha(), 8, 0.45f, 0.65f, 0.6f, 1.4f, 0.03f, 0.06f, leaf, leaf, leaf, 0.6f, 0.08f, 90f, 1.2f);
            AddStreaks(core, glow, travel, 0.34f, 0.03f, 0.06f, 0.26f, 0f);
            AddLight(new Color(0.55f, 0.72f, 1f, 1f), 4f, 2.0f, 0.10f);
        }

        /// <summary>Hero blue (the hero side of the locked VFX palette) for Lyra's staff sweep and arcane nova.</summary>
        private static readonly Color HeroBlueCore = new Color(0.80f, 0.90f, 1f, 1f);
        private static readonly Color HeroBlueGlow = new Color(0.30f, 0.58f, 1f, 1f);
        private static readonly Color HeroBlueEdge = new Color(0.12f, 0.30f, 0.85f, 0.8f);

        /// <summary>Lyra staff sweep hit: hero-blue flash, blue streaks thrown along the sweep, light motes, blue light.</summary>
        private void BuildHeroStaffSweep(Vector3 travel)
        {
            _life = 0.45f;
            Burst("Flash", Additive(), 1, 0.10f, 0.10f, 0f, 0f, 0.55f, 0.65f, Bright(HeroBlueCore), HeroBlueCore, HeroBlueGlow, 0f, 0.02f, 180f, 0.3f);
            Burst("Motes", Additive(), 18, 0.30f, 0.50f, 0.8f, 1.8f, 0.02f, 0.05f, Bright(HeroBlueCore), HeroBlueGlow, HeroBlueEdge, -0.5f, 0.08f, 90f, 0.5f);
            Burst("Sparks", Additive(), 14, 0.12f, 0.24f, 3.0f, 5.5f, 0.015f, 0.03f, Bright(HeroBlueCore), HeroBlueGlow, HeroBlueEdge, 0.3f, 0.04f, 60f, 0.3f);
            AddStreaks(HeroBlueCore, HeroBlueGlow, travel, 0.40f, 0.03f, 0.06f, 0.28f, 0f);
            AddLight(new Color(0.45f, 0.65f, 1f, 1f), 6f, 2.4f, 0.14f);
        }

        /// <summary>Lyra arcane nova hit: hero-blue flash, a wide flat ring of blue motes, rising light motes, blue light.</summary>
        private void BuildHeroArcaneNova()
        {
            _life = 0.6f;
            Burst("Flash", Additive(), 1, 0.12f, 0.12f, 0f, 0f, 0.80f, 0.95f, Bright(HeroBlueCore), HeroBlueCore, HeroBlueGlow, 0f, 0.02f, 180f, 0.3f);
            Burst("Ring", Additive(), 32, 0.30f, 0.45f, 2.6f, 3.6f, 0.03f, 0.06f, Bright(HeroBlueCore), HeroBlueGlow, HeroBlueEdge, 0f, 0.04f, 180f, 0.4f);
            Burst("Rise", Additive(), 20, 0.40f, 0.60f, 0.6f, 1.4f, 0.02f, 0.05f, Bright(HeroBlueCore), HeroBlueGlow, HeroBlueEdge, -0.9f, 0.10f, 90f, 0.6f);
            AddLight(new Color(0.40f, 0.62f, 1f, 1f), 8f, 3.0f, 0.20f);
        }

        /// <summary>
        /// Ironhowl overhead smash: villain-purple flash, a ring of jagged purple cracks laid on the battle
        /// floor (y = 0, where every lane slot stands) under the hit, dust and debris thrown out.
        /// </summary>
        private void BuildGroundSlam(Color core, Color glow, Color edge, Vector3 travel)
        {
            _life = 0.7f;
            var purple = new Color(0.62f, 0.28f, 1f, 1f);
            var purpleDeep = new Color(0.30f, 0.08f, 0.55f, 0.9f);
            var dust = new Color(0.40f, 0.34f, 0.30f, 0.55f);
            var dustEnd = new Color(0.30f, 0.26f, 0.22f, 0.2f);
            Burst("Flash", Additive(), 1, 0.12f, 0.12f, 0f, 0f, 0.70f, 0.85f, Bright(purple), purple, purpleDeep, 0f, 0.02f, 180f, 0.3f);
            Burst("Sparks", Additive(), 16, 0.15f, 0.30f, 3.0f, 5.5f, 0.015f, 0.035f, Bright(core), glow, edge, 0.6f, 0.05f, 60f, 0.3f);
            Burst("Dust", Alpha(), 14, 0.40f, 0.65f, 0.8f, 1.8f, 0.20f, 0.38f, dust, dustEnd, dustEnd, -0.05f, 0.25f, 120f, 2.0f);
            Burst("Debris", Alpha(), 12, 0.40f, 0.60f, 2.0f, 3.5f, 0.02f, 0.05f, dust, dust, dust, 2.5f, 0.10f, 70f, 1f);
            var origin = transform.position;
            origin.y = 0.012f;
            var side = Vector3.Cross(Vector3.up, travel).normalized;
            for (var i = 0; i < 9; i++)
            {
                var crack = Line("Crack" + i, Additive(), 6, 0.045f, purple, purpleDeep);
                crack.alignment = LineAlignment.TransformZ;
                crack.transform.rotation = Quaternion.LookRotation(Vector3.down, travel);
                var angle = (i / 9f) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                var dir = (travel * Mathf.Cos(angle)) + (side * Mathf.Sin(angle));
                var length = Random.Range(0.55f, 0.95f);
                for (var k = 0; k < 6; k++)
                {
                    var t = k / 5f;
                    var jag = k == 0 ? Vector3.zero : side * Random.Range(-0.06f, 0.06f) + travel * Random.Range(-0.06f, 0.06f);
                    var p = origin + (dir * (0.12f + t * length)) + jag;
                    p.y = origin.y;
                    crack.SetPosition(k, p);
                }
            }

            AddLight(new Color(0.65f, 0.30f, 1f, 1f), 8f, 3.0f, 0.22f);
        }

        /// <summary>
        /// Creature wing gust (attack 2 of Emberfang, Stormcrest and Ashwyrm), in the attacker's own colours: a soft flash,
        /// two sets of wind streaks swept across the hit (one level, one steep), swirling motes and a thin dust puff.
        /// </summary>
        private void BuildWingGust(Color core, Color glow, Color edge, Vector3 travel)
        {
            _life = 0.5f;
            var dust = new Color(0.55f, 0.50f, 0.45f, 0.35f);
            var dustEnd = new Color(0.45f, 0.42f, 0.38f, 0.1f);
            Burst("Flash", Additive(), 1, 0.10f, 0.10f, 0f, 0f, 0.50f, 0.60f, Bright(core), core, glow, 0f, 0.02f, 180f, 0.3f);
            Burst("Motes", Additive(), 22, 0.30f, 0.50f, 1.6f, 3.2f, 0.02f, 0.05f, Bright(core), glow, edge, -0.2f, 0.12f, 75f, 0.5f);
            Burst("Dust", Alpha(), 8, 0.35f, 0.55f, 0.6f, 1.4f, 0.18f, 0.34f, dust, dustEnd, dustEnd, -0.05f, 0.15f, 110f, 1.8f);
            AddStreaks(core, glow, travel, 0.60f, 0.09f, 0.10f, 0.36f, 8f);
            AddStreaks(glow, edge, travel, 0.45f, 0.07f, 0.14f, 0.40f, 62f);
            AddLight(glow, 4f, 2.2f, 0.14f);
        }

        /// <summary>
        /// Creature stomp quake (attack 3 of Emberfang, Stormcrest and Ashwyrm), in the attacker's own colours: a flash, a
        /// ring of jagged cracks on the battle floor under the hit, a dust ring, debris and sparks, a ground light.
        /// </summary>
        private void BuildStompQuake(Color core, Color glow, Color edge, Vector3 travel)
        {
            _life = 0.7f;
            var dust = new Color(0.40f, 0.34f, 0.30f, 0.55f);
            var dustEnd = new Color(0.30f, 0.26f, 0.22f, 0.2f);
            Burst("Flash", Additive(), 1, 0.12f, 0.12f, 0f, 0f, 0.60f, 0.75f, Bright(core), core, glow, 0f, 0.02f, 180f, 0.3f);
            Burst("Sparks", Additive(), 16, 0.15f, 0.30f, 3.0f, 5.5f, 0.015f, 0.035f, Bright(core), glow, edge, 0.6f, 0.05f, 60f, 0.3f);
            Burst("Dust", Alpha(), 16, 0.40f, 0.70f, 0.8f, 1.8f, 0.20f, 0.40f, dust, dustEnd, dustEnd, -0.05f, 0.25f, 120f, 2.0f);
            Burst("Debris", Alpha(), 12, 0.40f, 0.60f, 2.0f, 3.5f, 0.02f, 0.05f, dust, dust, dust, 2.5f, 0.10f, 70f, 1f);
            var origin = transform.position;
            origin.y = 0.012f;
            var side = Vector3.Cross(Vector3.up, travel).normalized;
            for (var i = 0; i < 8; i++)
            {
                var crack = Line("Crack" + i, Additive(), 6, 0.045f, Bright(core), glow);
                crack.alignment = LineAlignment.TransformZ;
                crack.transform.rotation = Quaternion.LookRotation(Vector3.down, travel);
                var angle = (i / 8f) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                var dir = (travel * Mathf.Cos(angle)) + (side * Mathf.Sin(angle));
                var length = Random.Range(0.45f, 0.85f) * _size;
                for (var k = 0; k < 6; k++)
                {
                    var t = k / 5f;
                    var jag = k == 0 ? Vector3.zero : (side * Random.Range(-0.06f, 0.06f)) + (travel * Random.Range(-0.06f, 0.06f));
                    var p = origin + (dir * ((0.10f * _size) + (t * length))) + (jag * _size);
                    p.y = origin.y;
                    crack.SetPosition(k, p);
                }
            }

            AddLight(glow, 7f, 2.8f, 0.22f);
        }

        /// <summary>
        /// Nightfang shadow claw wave (attack 3), in the attacker's own colours: three claw gouges ripping along the battle
        /// floor into the hit, a claw slash across it, dark wisps, sparks and a violet light.
        /// </summary>
        private void BuildClawWave(Color core, Color glow, Color edge, Vector3 travel)
        {
            _life = 0.55f;
            var dark = new Color(0.10f, 0.03f, 0.16f, 0.85f);
            var darker = new Color(0.04f, 0.01f, 0.07f, 0.6f);
            Burst("Flash", Additive(), 1, 0.08f, 0.08f, 0f, 0f, 0.40f, 0.50f, Bright(core), core, glow, 0f, 0.02f, 180f, 0.3f);
            Burst("Wisps", Alpha(), 10, 0.35f, 0.55f, 0.3f, 0.7f, 0.18f, 0.36f, dark, darker, darker, -0.15f, 0.12f, 120f, 2.0f);
            Burst("Sparks", Additive(), 12, 0.15f, 0.30f, 2.0f, 4.0f, 0.015f, 0.035f, Bright(core), glow, edge, 0.6f, 0.05f, 70f, 0.3f);
            AddStreaks(core, glow, travel, 0.30f, 0.06f, 0.09f, 0.38f, 55f);
            var origin = transform.position;
            origin.y = 0.012f;
            var side = Vector3.Cross(Vector3.up, travel).normalized;
            for (var i = 0; i < 3; i++)
            {
                var gouge = Line("Gouge" + i, Additive(), 5, 0.04f, glow, Bright(core));
                gouge.alignment = LineAlignment.TransformZ;
                gouge.transform.rotation = Quaternion.LookRotation(Vector3.down, travel);
                gouge.widthCurve = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0.3f));
                var lateral = side * ((i - 1) * 0.09f);
                for (var k = 0; k < 5; k++)
                {
                    var t = k / 4f;
                    var p = origin - (travel * ((1f - t) * 0.9f)) + lateral + (side * Random.Range(-0.015f, 0.015f));
                    p.y = origin.y;
                    gouge.SetPosition(k, p);
                }
            }

            AddLight(glow, 4f, 2.2f, 0.16f);
        }

        /// <summary>
        /// Oakenshield root snare: hero-blue flash, seven blue-glowing roots curling up out of the battle floor (y = 0) around
        /// the hit, leaves and an earth dust puff, blue light.
        /// </summary>
        private void BuildHeroRootSnare(Vector3 travel)
        {
            _life = 0.75f;
            var root = new Color(0.36f, 0.26f, 0.14f, 1f);
            var leaf = new Color(0.30f, 0.62f, 0.20f, 1f);
            var leafEnd = new Color(0.45f, 0.55f, 0.15f, 0.8f);
            var dust = new Color(0.42f, 0.34f, 0.24f, 0.55f);
            var dustEnd = new Color(0.34f, 0.28f, 0.20f, 0.25f);
            Burst("Flash", Additive(), 1, 0.12f, 0.12f, 0f, 0f, 0.60f, 0.75f, Bright(HeroBlueCore), HeroBlueCore, HeroBlueGlow, 0f, 0.02f, 180f, 0.3f);
            Burst("Leaves", Alpha(), 14, 0.45f, 0.65f, 1.0f, 2.2f, 0.04f, 0.08f, leaf, leafEnd, leafEnd, 1.0f, 0.10f, 80f, 1f);
            Burst("Dust", Alpha(), 10, 0.45f, 0.70f, 0.4f, 1.0f, 0.22f, 0.42f, dust, dustEnd, dustEnd, -0.05f, 0.25f, 120f, 1.8f);
            Burst("Motes", Additive(), 16, 0.40f, 0.60f, 0.5f, 1.2f, 0.02f, 0.05f, Bright(HeroBlueCore), HeroBlueGlow, HeroBlueEdge, -0.8f, 0.20f, 90f, 0.6f);
            var origin = transform.position;
            origin.y = 0f;
            var side = Vector3.Cross(Vector3.up, travel).normalized;
            for (var i = 0; i < 7; i++)
            {
                var angle = (i / 7f) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
                var dir = (travel * Mathf.Cos(angle)) + (side * Mathf.Sin(angle));
                var foot = origin + (dir * Random.Range(0.25f, 0.40f));
                var height = Random.Range(0.55f, 0.95f);
                var bark = Line("Root" + i, Alpha(), 7, 0.07f, root, root);
                var glowLine = Line("RootGlow" + i, Additive(), 7, 0.035f, HeroBlueGlow, Bright(HeroBlueCore));
                for (var k = 0; k < 7; k++)
                {
                    var t = k / 6f;
                    // Rises from the floor and curls in over the target.
                    var p = foot - (dir * (0.22f * t * t)) + (Vector3.up * (height * t)) + (side * (0.05f * Mathf.Sin(t * 6f + i)));
                    bark.SetPosition(k, p);
                    glowLine.SetPosition(k, p);
                }
            }

            AddLight(new Color(0.40f, 0.62f, 1f, 1f), 7f, 2.6f, 0.20f);
        }

        /// <summary>
        /// Sir Aldric shield bash hit: hero-blue flash, two blue shock rings standing across the hit (facing the bash), a flat
        /// ring of blue motes thrown out sideways, steel sparks, blue light.
        /// </summary>
        private void BuildHeroShieldShock(Vector3 travel)
        {
            _life = 0.5f;
            Burst("Flash", Additive(), 1, 0.10f, 0.10f, 0f, 0f, 0.70f, 0.80f, Bright(HeroBlueCore), HeroBlueCore, HeroBlueGlow, 0f, 0.02f, 180f, 0.3f);
            Burst("Ring", Additive(), 28, 0.25f, 0.40f, 2.4f, 3.4f, 0.025f, 0.05f, Bright(HeroBlueCore), HeroBlueGlow, HeroBlueEdge, 0f, 0.04f, 180f, 0.4f);
            Burst("Sparks", Additive(), 12, 0.12f, 0.24f, 3.0f, 5.0f, 0.015f, 0.03f, Bright(HeroBlueCore), HeroBlueGlow, HeroBlueEdge, 0.4f, 0.04f, 50f, 0.3f);
            var origin = transform.position;
            var side = Vector3.Cross(Vector3.up, travel).normalized;
            for (var r = 0; r < 2; r++)
            {
                var radius = r == 0 ? 0.28f : 0.45f;
                var ring = Line("Shock" + r, Additive(), 25, r == 0 ? 0.05f : 0.03f, Bright(HeroBlueCore), HeroBlueGlow);
                ring.alignment = LineAlignment.TransformZ;
                ring.transform.rotation = Quaternion.LookRotation(travel, Vector3.up);
                for (var k = 0; k < 25; k++)
                {
                    var a = (k / 24f) * Mathf.PI * 2f;
                    ring.SetPosition(k, origin + (travel * (0.06f * r)) + (side * (Mathf.Cos(a) * radius)) + (Vector3.up * (Mathf.Sin(a) * radius)));
                }
            }

            AddLight(new Color(0.40f, 0.62f, 1f, 1f), 8f, 2.8f, 0.16f);
        }

        /// <summary>Villain purple (the villain side of the locked VFX palette) for Vespera's shadow lash and hex rain.</summary>
        private static readonly Color VillainPurpleCore = new Color(0.86f, 0.70f, 1f, 1f);
        private static readonly Color VillainPurpleGlow = new Color(0.62f, 0.28f, 1f, 1f);
        private static readonly Color VillainPurpleEdge = new Color(0.30f, 0.08f, 0.55f, 0.9f);
        private static readonly Color VillainWisp = new Color(0.16f, 0.05f, 0.22f, 0.75f);
        private static readonly Color VillainWispEnd = new Color(0.08f, 0.02f, 0.11f, 0.3f);

        /// <summary>Vespera shadow lash hit: violet flash, purple streaks thrown along the lash, sparks, dark wisps, violet light.</summary>
        private void BuildVillainShadowLash(Vector3 travel)
        {
            _life = 0.5f;
            Burst("Flash", Additive(), 1, 0.10f, 0.10f, 0f, 0f, 0.55f, 0.65f, Bright(VillainPurpleCore), VillainPurpleCore, VillainPurpleGlow, 0f, 0.02f, 180f, 0.3f);
            Burst("Sparks", Additive(), 14, 0.12f, 0.26f, 3.0f, 5.5f, 0.015f, 0.03f, Bright(VillainPurpleCore), VillainPurpleGlow, VillainPurpleEdge, 0.3f, 0.04f, 60f, 0.3f);
            Burst("Wisps", Alpha(), 10, 0.35f, 0.55f, 0.3f, 0.7f, 0.18f, 0.34f, VillainWisp, VillainWispEnd, VillainWispEnd, -0.15f, 0.12f, 120f, 2.0f);
            AddStreaks(VillainPurpleCore, VillainPurpleGlow, travel, 0.45f, 0.03f, 0.06f, 0.30f, -20f);
            AddLight(new Color(0.65f, 0.30f, 1f, 1f), 6f, 2.4f, 0.14f);
        }

        /// <summary>
        /// Vespera hex rain hit: a purple hexagon glyph laid on the battle floor (y = 0) under the hit, violet motes falling into
        /// it from above, a flash, dark wisps, violet light.
        /// </summary>
        private void BuildVillainHexRain(Vector3 travel)
        {
            _life = 0.6f;
            Burst("Flash", Additive(), 1, 0.12f, 0.12f, 0f, 0f, 0.60f, 0.70f, Bright(VillainPurpleCore), VillainPurpleCore, VillainPurpleGlow, 0f, 0.02f, 180f, 0.3f);
            Burst("Wisps", Alpha(), 8, 0.40f, 0.60f, 0.3f, 0.7f, 0.18f, 0.34f, VillainWisp, VillainWispEnd, VillainWispEnd, -0.15f, 0.15f, 120f, 2.0f);
            var origin = transform.position;
            var fall = Line("Fall", Additive(), 2, 0.05f, Bright(VillainPurpleCore), VillainPurpleGlow);
            fall.SetPosition(0, origin + (Vector3.up * 1.2f));
            fall.SetPosition(1, origin);
            origin.y = 0.012f;
            var side = Vector3.Cross(Vector3.up, travel).normalized;
            var hex = Line("Hex", Additive(), 7, 0.035f, VillainPurpleGlow, Bright(VillainPurpleCore));
            hex.alignment = LineAlignment.TransformZ;
            hex.transform.rotation = Quaternion.LookRotation(Vector3.down, travel);
            for (var k = 0; k < 7; k++)
            {
                var a = (k / 6f) * Mathf.PI * 2f;
                hex.SetPosition(k, origin + (travel * (Mathf.Cos(a) * 0.32f)) + (side * (Mathf.Sin(a) * 0.32f)));
            }

            AddLight(new Color(0.65f, 0.30f, 1f, 1f), 7f, 2.6f, 0.18f);
        }

        /// <summary>Bonequill bone arrow hit: violet flash, pale bone shards, dark purple wisps.</summary>
        private void BuildBoneShard(Color core, Color glow, Color edge, Vector3 travel)
        {
            _life = 0.5f;
            var bone = new Color(0.86f, 0.82f, 0.70f, 1f);
            var wisp = new Color(0.16f, 0.05f, 0.22f, 0.75f);
            var wispEnd = new Color(0.08f, 0.02f, 0.11f, 0.3f);
            Burst("Flash", Additive(), 1, 0.10f, 0.10f, 0f, 0f, 0.45f, 0.55f, Bright(core), core, glow, 0f, 0.02f, 180f, 0.25f);
            Burst("Shards", Alpha(), 12, 0.35f, 0.55f, 1.8f, 3.4f, 0.02f, 0.05f, bone, bone, bone, 2.5f, 0.06f, 70f, 1f);
            Burst("Wisps", Alpha(), 8, 0.35f, 0.55f, 0.3f, 0.7f, 0.16f, 0.32f, wisp, wispEnd, wispEnd, -0.15f, 0.10f, 120f, 2.0f);
            Burst("Motes", Additive(), 12, 0.25f, 0.45f, 1.5f, 3.0f, 0.02f, 0.04f, glow, edge, edge, 0.3f, 0.05f, 70f, 0.4f);
            AddStreaks(core, glow, travel, 0.30f, 0.03f, 0.06f, 0.28f, 0f);
            AddLight(new Color(0.70f, 0.35f, 0.95f, 1f), 4f, 2.0f, 0.12f);
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

        /// <summary>Ashwyrm void-fire fireball hit: violet-white flash, purple flame puff, a gust of dark smoke pushed along the hit, embers, violet light.</summary>
        private void BuildVoidFireBreath(Color core, Color glow, Color edge, Vector3 travel)
        {
            _life = 0.5f;
            var smoke = new Color(0.12f, 0.05f, 0.18f, 0.75f);
            var smokeEnd = new Color(0.06f, 0.02f, 0.09f, 0.3f);
            Burst("Flash", Additive(), 1, 0.12f, 0.12f, 0f, 0f, 0.60f, 0.70f, Bright(core), core, glow, 0f, 0.02f, 180f, 0.25f);
            Burst("Flame", Additive(), 28, 0.22f, 0.40f, 1.0f, 2.8f, 0.14f, 0.32f, core, glow, edge, -0.25f, 0.10f, 70f, 1.6f);
            Burst("Gust", Alpha(), 8, 0.35f, 0.50f, 0.6f, 1.2f, 0.20f, 0.40f, smoke, smokeEnd, smokeEnd, -0.1f, 0.12f, 100f, 2.0f);
            Burst("Embers", Additive(), 16, 0.35f, 0.60f, 2.4f, 4.4f, 0.02f, 0.05f, glow, edge, edge, 0.9f, 0.05f, 60f, 0.4f);
            AddLight(new Color(0.70f, 0.35f, 1f, 1f), 7f, 2.6f, 0.18f);
        }

        /// <summary>Vespera shadow orb hit: violet flash, a ring of purple motes, dark wisps, rune-bright sparks.</summary>
        private void BuildArcaneConjure(Color core, Color glow, Color edge)
        {
            _life = 0.45f;
            var dark = new Color(0.12f, 0.03f, 0.20f, 0.8f);
            var darker = new Color(0.05f, 0.01f, 0.09f, 0.5f);
            Burst("Flash", Additive(), 1, 0.10f, 0.10f, 0f, 0f, 0.55f, 0.65f, Bright(core), core, glow, 0f, 0.02f, 180f, 0.3f);
            Burst("Motes", Additive(), 22, 0.25f, 0.40f, 1.2f, 2.4f, 0.03f, 0.07f, Bright(core), glow, edge, 0f, 0.06f, 180f, 0.6f);
            Burst("Wisps", Alpha(), 8, 0.30f, 0.45f, 0.3f, 0.7f, 0.18f, 0.34f, dark, darker, darker, -0.15f, 0.10f, 120f, 2.0f);
            Burst("Sparks", Additive(), 10, 0.12f, 0.25f, 3.0f, 5.0f, 0.015f, 0.03f, Bright(core), glow, edge, 0.4f, 0.04f, 60f, 0.3f);
            AddLight(new Color(0.65f, 0.30f, 1f, 1f), 6f, 2.2f, 0.15f);
        }

        /// <summary>Lyra arcane bolt hit: white-gold flash, a soft halo, rising light motes, warm light.</summary>
        private void BuildStaffLight(Color core, Color glow, Color edge)
        {
            _life = 0.45f;
            Burst("Flash", Additive(), 1, 0.10f, 0.10f, 0f, 0f, 0.65f, 0.75f, Bright(core), core, glow, 0f, 0.02f, 180f, 0.3f);
            Burst("Halo", Additive(), 4, 0.20f, 0.30f, 0.1f, 0.3f, 0.35f, 0.50f, glow, edge, edge, 0f, 0.05f, 180f, 1.5f);
            Burst("Motes", Additive(), 20, 0.35f, 0.55f, 0.8f, 1.8f, 0.02f, 0.05f, Bright(core), glow, edge, -0.6f, 0.08f, 90f, 0.5f);
            Burst("Sparks", Additive(), 12, 0.12f, 0.22f, 3.0f, 5.0f, 0.015f, 0.03f, Bright(core), glow, edge, 0.3f, 0.04f, 50f, 0.3f);
            AddLight(new Color(1f, 0.92f, 0.65f, 1f), 7f, 2.4f, 0.14f);
        }

        /// <summary>Oakenshield thorn spear hit: green flash, leaves and splinters thrown along the hit, an earth dust puff.</summary>
        private void BuildLeafEarth(Color core, Color glow, Color edge, Vector3 travel)
        {
            _life = 0.55f;
            var leaf = new Color(0.30f, 0.62f, 0.20f, 1f);
            var leafEnd = new Color(0.45f, 0.55f, 0.15f, 0.8f);
            var dust = new Color(0.42f, 0.34f, 0.24f, 0.55f);
            var dustEnd = new Color(0.34f, 0.28f, 0.20f, 0.25f);
            var bark = new Color(0.30f, 0.20f, 0.12f, 1f);
            Burst("Flash", Additive(), 1, 0.08f, 0.08f, 0f, 0f, 0.45f, 0.55f, Bright(core), core, glow, 0f, 0.02f, 180f, 0.25f);
            Burst("Leaves", Alpha(), 16, 0.45f, 0.60f, 1.2f, 2.6f, 0.04f, 0.08f, leaf, leafEnd, leafEnd, 1.2f, 0.08f, 70f, 1f);
            Burst("Splinters", Alpha(), 10, 0.30f, 0.50f, 2.0f, 3.5f, 0.02f, 0.04f, bark, bark, bark, 2.5f, 0.05f, 55f, 1f);
            Burst("Dust", Alpha(), 7, 0.40f, 0.55f, 0.3f, 0.8f, 0.22f, 0.45f, dust, dustEnd, dustEnd, -0.05f, 0.15f, 120f, 1.8f);
            AddStreaks(core, glow, travel, 0.26f, 0.04f, 0.07f, 0.28f, 30f);
            AddLight(new Color(0.55f, 0.90f, 0.40f, 1f), 4f, 2.0f, 0.12f);
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

            TickFlight();

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
                var length = _arcLength * _size * Random.Range(0.6f, 1f) * Mathf.Max(0.35f, strength);
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
            length *= _size;
            spacing *= _size;
            var origin = transform.position - (travel * (0.04f * _size));
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
            go.transform.localPosition = new Vector3(0f, 0f, 0.15f * _size);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range * _size;
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
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin * _size, speedMax * _size);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin * _size, sizeMax * _size);
            main.startColor = Color.white;
            main.gravityModifier = gravity * _size;
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

            shape.radius = Mathf.Max(radius * _size, 0.0001f);
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
            line.widthMultiplier = width * _size;
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
