using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// How a real player sidesteps enemy shots (Derek 2026-10-29: ranged hits kill a follower in 2, "they are easily
/// dodgeable"; "teach the test player to dodge shots similarly to how a real player would").
/// <para>
/// A shot is only seen once it has been in the air for a human reaction time (0.25-0.35 s, fixed per shot from its id
/// so runs are deterministic). For every seen shot still on its way, the player works out where it will cross the
/// squad, using the squad's current shape around the hero, and so which hero positions would put a body in its lane.
/// He then keeps the position he wanted (lining up a box or a foe) unless that is in a lane, in which case he steers to
/// the nearest position that dodges the most shots. Steering goes through the normal steer speed and smoothing, so a
/// late or wide shot still lands: nothing is teleported and no flat dodge rate is applied. A dodge outranks a box
/// for the second or so the shot is coming (a 13-24 damage hit costs more than a late box), and the boss's slam and
/// quake dodges (handled by the caller) outrank both.
/// </para>
/// </summary>
public static class ShotDodge
{
    public const float ReactionMin = 0.25f;
    public const float ReactionSpread = 0.10f;

    /// <summary>Only shots landing within this many seconds are worth reacting to.</summary>
    public const float Horizon = 1.0f;

    /// <summary>How far clear of a lane the player aims (people overshoot a little, not a hair).</summary>
    public const float Margin = 0.15f;

    public static float ReactionFor(RunnerShot s)
    {
        // Deterministic per-shot jitter: a small integer hash of the id onto [0, 1).
        var h = (uint)s.Id * 2654435761u;
        return ReactionMin + (ReactionSpread * ((h >> 8) % 1000u) / 1000f);
    }

    public static float Steer(RunnerRun run, float wanted)
    {
        var pace = run.Halted ? 0f : run.Pace;
        var hero = run.Hero.X;
        var lanes = new List<(float Lo, float Hi)>();
        foreach (var s in run.Shots)
        {
            if (!s.ByFoe || s.Speed <= 0f || s.Travelled / s.Speed < ReactionFor(s))
            {
                continue;
            }

            var vz = (s.DZ * s.Speed) - pace;
            foreach (var m in run.Squad)
            {
                if (Math.Abs(vz) < 1e-3f)
                {
                    continue;
                }

                var t = (m.Z - s.Z) / vz;
                if (t < 0f || t > Horizon)
                {
                    continue;
                }

                var x = s.X + (s.DX * s.Speed * t);
                var r = m.Unit.Radius + RunnerRules.ShotHalfWidth + Margin;
                var off = m.X - hero;
                lanes.Add((x - off - r, x - off + r));
            }
        }

        if (lanes.Count == 0)
        {
            return wanted;
        }

        int Hits(float x) => lanes.Count(l => x > l.Lo && x < l.Hi);
        // Never sidesteps into a lane trap or across one it is level with: the player's trap line wins.
        bool Trapped(float x) => Math.Abs(Runner.Dodge(run, x) - x) > 0.05f;
        var max = run.MaxSteer;
        var candidates = new List<float> { Math.Clamp(wanted, -max, max) };
        foreach (var l in lanes)
        {
            candidates.Add(Math.Clamp(l.Lo - 1e-3f, -max, max));
            candidates.Add(Math.Clamp(l.Hi + 1e-3f, -max, max));
        }

        // Fewest bodies in a lane first, then the shortest move from where he wanted to be.
        return candidates.OrderBy(Trapped).ThenBy(Hits).ThenBy(c => Math.Abs(c - wanted)).ThenBy(c => Math.Abs(c - hero)).First();
    }
}
