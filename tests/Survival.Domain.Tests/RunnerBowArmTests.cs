#nullable enable
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

        [Theory]
        [InlineData(0f)]
        [InlineData(0.5f)]
        [InlineData(1f)]
        public void DrawElbowRidesBackAndUpBehindTheShoulder(float draw)
        {
            var grip = LeftShoulder + (R * RunnerWeaponAim.BowHold.X) + (Up * RunnerWeaponAim.BowHold.Y) + (F * RunnerWeaponAim.BowHold.Z);
            var nock = grip - (F * RunnerWeaponAim.NockBehind);
            var full = grip - (F * RunnerWeaponAim.FullDrawBehind) + (Up * RunnerWeaponAim.FullDrawUp);
            var at = Vector3.Lerp(nock, full, draw);
            RunnerTwoBoneIk.Solve(RightShoulder, UpperArm, Forearm, at, RightShoulder + RunnerWeaponAim.DrawElbowPole(R, Up, F), out var elbow, out var wrist);
            var bend = elbow - ((RightShoulder + wrist) * 0.5f);
            if (bend.Length() > 0.02f)
            {
                // A bent draw arm folds back and up, never forward or down (a backwards elbow).
                Assert.True(bend.Z < 0f, $"draw elbow should point back, bend {bend}");
                Assert.True(bend.Y > -0.01f, $"draw elbow should not drop under the arm, bend {bend}");
            }

            if (draw >= 1f)
            {
                Assert.True(elbow.Z < RightShoulder.Z + 0.05f, $"at full draw the elbow is back level with or behind the shoulder, elbow {elbow}");
                Assert.True(elbow.Y > RightShoulder.Y - 0.12f, $"at full draw the elbow rides high, elbow {elbow}");
            }
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
