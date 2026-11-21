#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Roster;

namespace Survival.Domain.Runner
{
    /// <summary>Follower slots behind the hero: row r (1, 2, 3 ...) holds r + 1 units, centred, <see cref="RunnerRules.Spacing"/> apart.</summary>
    /// <summary>Critically damped spring used for steering (the same maths as Unity's Mathf.SmoothDamp).</summary>
    public static class RunnerSteering
    {
        public static float SmoothDamp(float current, float target, ref float velocity, float smoothTime, float maxSpeed, float dt)
        {
            if (dt <= 0f)
            {
                return current;
            }

            smoothTime = Math.Max(0.0001f, smoothTime);
            var omega = 2f / smoothTime;
            var x = omega * dt;
            var exp = 1f / (1f + x + (0.48f * x * x) + (0.235f * x * x * x));
            var change = current - target;
            var original = target;
            var maxChange = maxSpeed * smoothTime;
            change = change < -maxChange ? -maxChange : change > maxChange ? maxChange : change;
            target = current - change;
            var temp = (velocity + (omega * change)) * dt;
            velocity = (velocity - (omega * temp)) * exp;
            var output = target + ((change + temp) * exp);
            if ((original - current > 0f) == (output > original))
            {
                output = original;
                velocity = (output - original) / dt;
            }

            return output;
        }
    }

    /// <summary>
    /// Where the road camera frames the squad. It rides halfway between the lead and the back row, pushed one row
    /// up the road (Derek 2026-10-23: "the heros can be pushed back a row. now that we limited it to 10 there's an
    /// empty row in the back"): with the squad capped at 10 the back row is row 3, and the bottom of the shot used to
    /// leave a row and a half of empty road behind it. Only the camera moves; the sim (and so the engage distance,
    /// shot range, the arena line and the quake lanes) is unchanged.
    /// </summary>
    public static class RunnerFraming
    {
        public const float CameraHeight = 7.6f;
        public const float CameraBack = 10.0f;
        public const float CameraLookAhead = 6.5f;
        public const float CameraLookHeight = 0.9f;
        public const float FieldOfViewDegrees = 52f;

        /// <summary>
        /// Share of a tall phone's extra view that goes under the authored 9:16 frame (see <see cref="View.AspectFit"/>).
        /// 1: the top of the shot (title row, horizon, the road ahead) sits exactly as on the 9:16 Game view and the extra
        /// height is road under the squad, where the thumb steers; 0 would keep the back row on the bottom spot and add
        /// it all as sky above the horizon instead (a quarter of a 19.5:9 screen).
        /// </summary>
        public const float ExtraViewBelow = 1f;

        /// <summary>One formation row (see <see cref="RunnerFormation.Offset"/>).</summary>
        public const float RowMetres = RunnerRules.Spacing * 0.95f;

        /// <summary>Formation rows behind the lead in a full squad (<see cref="RunnerRules.MaxFollowers"/> followers).</summary>
        public static int FullRows => RunnerFormation.RowOf(RunnerRules.MaxFollowers - 1);

        /// <summary>
        /// Where the back row sits from the focus (metres, - = below/behind): the same spot whatever the squad's size.
        /// It is the full squad's framing (halfway between lead and back row, one row up the road), so ten heroes fill
        /// the shot exactly as before; a smaller squad keeps its rearmost row on that bottom spot and fills forward
        /// from it (Derek 2026-10-23: "the 2 in the back sit all the way down with the lead in front").
        /// </summary>
        public static float BackRowFromFocus => (-FullRows * RowMetres * 0.5f) - RowMetres;

        /// <summary>The focus's place along the road with the rearmost member at <paramref name="backZ"/> (bottom-anchored).</summary>
        public static float FocusZ(float backZ) => backZ - BackRowFromFocus;

        /// <summary>The rearmost member's place along the road (the lead's when it walks alone).</summary>
        public static float BackZ(IReadOnlyList<RunnerMember> squad, float leadZ)
        {
            var back = leadZ;
            for (var i = 0; i < squad.Count; i++)
            {
                back = Math.Min(back, squad[i].Z);
            }

            return back;
        }

        /// <summary>The back row's offset from the lead once a squad of <paramref name="members"/> has settled into its slots.</summary>
        public static float SettledBackOffset(int members)
        {
            var back = 0f;
            for (var slot = 0; slot < members - 1; slot++)
            {
                back = Math.Min(back, RunnerFormation.Offset(slot).Z);
            }

            return back;
        }

        /// <summary>Road place (metres from the focus, - = behind) where the bottom edge of the shot meets the ground.</summary>
        public static float BottomGroundFromFocus(bool boss) => BottomGroundFromFocus(boss, View.AspectFit.ReferenceAspect);

        /// <summary>The same on a screen <paramref name="aspect"/> wide-per-tall (the camera's <see cref="View.AspectFit"/> frustum).</summary>
        public static float BottomGroundFromFocus(bool boss, float aspect)
        {
            var height = CameraHeight + (boss ? 0.8f : 0f);
            var back = CameraBack + (boss ? 1.4f : 0f);
            var lookHeight = CameraLookHeight + (boss ? 0.6f : 0f);
            var pitch = Math.Atan2(height - lookHeight, back + CameraLookAhead);
            var frustum = View.AspectFit.Fit(FieldOfViewDegrees, aspect, ExtraViewBelow);
            var down = pitch + Math.Atan(-frustum.Bottom);
            return (float)(-back + (height / Math.Tan(down)));
        }
    }

    public static class RunnerFormation
    {
        public static int RowOf(int slot)
        {
            var row = 1;
            var start = 0;
            while (slot >= start + row + 1)
            {
                start += row + 1;
                row++;
            }

            return row;
        }

        /// <summary>(x, z) offset from the hero; z is negative (behind).</summary>
        public static (float X, float Z) Offset(int slot)
        {
            if (slot < 0)
            {
                return (0f, 0f);
            }

            var row = 1;
            var start = 0;
            while (slot >= start + row + 1)
            {
                start += row + 1;
                row++;
            }

            var k = slot - start;
            var s = RunnerRules.Spacing;
            return ((k - (row * 0.5f)) * s, -row * s * 0.95f);
        }
    }
}
