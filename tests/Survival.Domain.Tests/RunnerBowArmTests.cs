#nullable enable
using System;
using System.Numerics;
using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests
{
    /// <summary>Rowan's bow arms bend the right way (Derek 2026-10-27: "the archer arms look backwards now when he fires").</summary>
    public sealed class RunnerBowArmTests
    {
        // Unity convention: the hero faces +z, up is +y, right is up x forward = +x.
        private static readonly Vector3 F = Vector3.UnitZ;
        private static readonly Vector3 Up = Vector3.UnitY;
        private static readonly Vector3 R = Vector3.Cross(Up, F);
        private static readonly Vector3 LeftShoulder = new Vector3(-0.18f, 1.45f, 0f);
        private static readonly Vector3 RightShoulder = new Vector3(0.18f, 1.45f, 0f);
        private const float UpperArm = 0.28f;
        private const float Forearm = 0.26f;

        [Fact]
        public void RightIsPlusX()
        {
            Assert.Equal(1f, R.X, 3);
        }

        [Fact]
        public void BowElbowSitsOutAndDownNotInsideTheArm()
        {
            var hold = LeftShoulder + (R * RunnerWeaponAim.BowHold.X) + (Up * RunnerWeaponAim.BowHold.Y) + (F * (RunnerWeaponAim.BowHold.Z - 0.12f));
            RunnerTwoBoneIk.Solve(LeftShoulder, UpperArm, Forearm, hold, LeftShoulder + RunnerWeaponAim.BowElbowPole(R, Up, F), out var elbow, out _);
            var mid = (LeftShoulder + hold) * 0.5f;
            var bend = elbow - mid;
            Assert.True(bend.X < 0f, $"bow elbow should bow out to the left, bend {bend}");
            Assert.True(bend.Y <= 0f, $"bow elbow should not point up, bend {bend}");
        }

        // The head: centred over the shoulders' midpoint, top at 1.75 m, about 0.11 m in radius.
        private const float HeadTop = 1.75f;
        private const float HeadRadius = 0.11f;
        private static readonly Vector3 ShoulderMid = new Vector3(0f, 1.45f, 0f);

        // The shoulders once the upper body is twisted (Bonequill's draw): the left forward, the right back.
        private static Vector3 TwistedLeft => ShoulderMid - (RunnerWeaponAim.ShoulderLine(R, F) * 0.18f);
        private static Vector3 TwistedRight => ShoulderMid + (RunnerWeaponAim.ShoulderLine(R, F) * 0.18f);

        private static Vector3 Grip => TwistedLeft + (R * RunnerWeaponAim.BowHold.X) + (Up * RunnerWeaponAim.BowHold.Y) + (F * RunnerWeaponAim.BowHold.Z);

        [Fact]
        public void TorsoTwistsSoTheChestFacesSlightlyRight()
        {
            // Derek 2026-10-28: "his upper torso should be twisted so that his chest is slightly facing right".
            var chest = RunnerWeaponAim.ChestFacing(R, F);
            var yaw = MathF.Atan2(chest.X, chest.Z) * 180f / MathF.PI;
            Assert.InRange(yaw, 25f, 35f);
            Assert.True(TwistedRight.Z < TwistedLeft.Z, "the right (draw) shoulder swings back, the left toward the enemy");
            Assert.Equal(0f, Vector3.Dot(RunnerWeaponAim.ShoulderLine(R, F), chest), 4);
        }

        [Theory]
        [InlineData(0f, 0f, UpperArm, Forearm)]
        [InlineData(0.5f, 0f, UpperArm, Forearm)]
        [InlineData(1f, 0f, UpperArm, Forearm)]
        [InlineData(1f, 1f, UpperArm, Forearm)]
        // Rowan's own rig: a long upper arm, a short forearm (0.32 / 0.17 m of a 0.50 m arm, scaled to the 0.55 m design arm).
        [InlineData(0f, 0f, 0.357f, 0.193f)]
        [InlineData(0.5f, 0f, 0.357f, 0.193f)]
        [InlineData(1f, 0f, 0.357f, 0.193f)]
        [InlineData(1f, 1f, 0.357f, 0.193f)]
        public void DrawArmBendsBackBehindTheShoulderNeverOverOrBehindTheHead(float draw, float flick, float upperArm, float forearm)
        {
            // Derek 2026-10-28: "his right arm should be slightly behind him bent at the elbow that he can pull the string backwards".
            var at = RunnerWeaponAim.DrawHand(Grip, TwistedRight, R, Up, F, draw, flick, 1f);
            RunnerTwoBoneIk.Solve(TwistedRight, upperArm, forearm, at, TwistedRight + RunnerWeaponAim.DrawElbowPole(R, Up, F), out var elbow, out var wrist);
            Assert.True(wrist.Y < HeadTop && elbow.Y < HeadTop, $"nothing over the head, wrist {wrist} elbow {elbow}");
            Assert.True(elbow.Y <= TwistedRight.Y + 0.03f, $"draw elbow at or under shoulder height, elbow {elbow}");
            // Never tucked behind the head: anything behind the shoulders stays out wide of the head.
            if (elbow.Z < 0f)
            {
                Assert.True(elbow.X > HeadRadius + 0.07f, $"draw elbow out to the right, not behind the head, elbow {elbow}");
            }

            if (draw >= 1f)
            {
                Assert.True(elbow.Z < TwistedRight.Z - 0.02f, $"bent elbow points back behind the shoulder plane, elbow {elbow}");
                Assert.InRange(elbow.Y, TwistedRight.Y - 0.15f, TwistedRight.Y + 0.03f);
            }

            if (draw >= 1f && flick == 0f)
            {
                Assert.True(Vector3.Distance(wrist, TwistedRight) < 0.28f, $"string drawn to the right shoulder, wrist {wrist}");
                Assert.True(wrist.Z < Grip.Z - 0.35f, $"string pulled back along the arrow line, wrist {wrist} grip {Grip}");
            }
        }

        [Fact]
        public void BowHandIsForwardSlightlyRightOfCentreAtShoulderHeight()
        {
            RunnerTwoBoneIk.Solve(TwistedLeft, UpperArm, Forearm, Grip, TwistedLeft + RunnerWeaponAim.BowElbowPole(R, Up, F), out var elbow, out var wrist);
            Assert.True(Vector3.Distance(wrist, Grip) < 0.01f, $"bow arm reaches the grip, wrist {wrist} grip {Grip}");
            Assert.True(wrist.Z > ShoulderMid.Z + 0.45f, $"bow arm out toward the enemy, wrist {wrist}");
            Assert.InRange(wrist.X, 0f, 0.12f);
            Assert.InRange(wrist.Y, ShoulderMid.Y - 0.1f, ShoulderMid.Y + 0.1f);
            Assert.True(elbow.Y < HeadTop - 0.2f);
        }

        [Fact]
        public void BowDepthGuessedBackwardsIsFlippedNotSpunInTheWrist()
        {
            // The riser pointing at the archer (a wrong sign from an unreadable mesh) is turned the short way.
            Assert.True(RunnerWeaponAim.FlipBowDepth(-F, Up, F, Up));
            Assert.False(RunnerWeaponAim.FlipBowDepth(F, Up, F, Up));
            Assert.False(RunnerWeaponAim.FlipBowDepth(Vector3.Normalize(F + (R * 0.3f)), Up, F, Up));
            Assert.True(RunnerWeaponAim.FlipBowDepth(Vector3.Normalize(-F + (R * 0.3f)), Up, F, Up));
        }

        [Fact]
        public void LyrasStaffArmSolveIsPinned()
        {
            // Lyra is fine (Derek 2026-10-27): her hold and elbow hint are untouched by the bow fix.
            Assert.Equal(new Vector3(0.10f, -0.28f, 0.30f), RunnerWeaponAim.StaffHold);
            var s = RunnerWeaponAim.StaffHold;
            var hold = RightShoulder + (R * s.X) + (Up * s.Y) + (F * s.Z);
            var pole = RightShoulder + (R * 0.30f) - (Up * 0.40f) - (F * 0.10f);
            RunnerTwoBoneIk.Solve(RightShoulder, UpperArm, Forearm, hold, pole, out var elbow, out var wrist);
            Assert.Equal(hold.X, wrist.X, 5);
            Assert.Equal(hold.Z, wrist.Z, 5);
            Assert.True(elbow.X > RightShoulder.X && elbow.Y < RightShoulder.Y);
        }
    }
}
