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

        // The head: centred over the shoulders, its back at the shoulder line's depth (z 0), top at 1.75 m.
        private const float HeadPlaneZ = 0f;
        private const float HeadTop = 1.75f;

        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(0.5f, 0f)]
        [InlineData(1f, 0f)]
        [InlineData(1f, 1f)]
        public void DrawArmStaysInFrontOfTheHeadAndUnderShoulderHeight(float draw, float flick)
        {
            // Derek 2026-10-28: "his right arm is bent completely behind his head in a non-natural position".
            var grip = LeftShoulder + (R * RunnerWeaponAim.BowHold.X) + (Up * RunnerWeaponAim.BowHold.Y) + (F * RunnerWeaponAim.BowHold.Z);
            var at = RunnerWeaponAim.DrawHand(grip, RightShoulder, R, Up, F, draw, flick, 1f);
            RunnerTwoBoneIk.Solve(RightShoulder, UpperArm, Forearm, at, RightShoulder + RunnerWeaponAim.DrawElbowPole(R, Up, F), out var elbow, out var wrist);
            Assert.True(wrist.Z > HeadPlaneZ + 0.05f, $"string hand in front of the head, wrist {wrist}");
            Assert.True(elbow.Z > HeadPlaneZ - 0.02f, $"draw elbow not behind the head, elbow {elbow}");
            Assert.True(wrist.Y < HeadTop && elbow.Y < HeadTop, $"nothing over the head, wrist {wrist} elbow {elbow}");
            Assert.True(elbow.Y <= RightShoulder.Y + 0.02f, $"draw elbow at or under shoulder height, elbow {elbow}");
            // At the nock the arm reaches across the chest (fine: "cross his body or in front"); once drawn the elbow is on the right.
            if (draw >= 0.5f) Assert.True(elbow.X > 0f, $"draw elbow stays on the right side, elbow {elbow}");
            if (draw >= 1f && flick == 0f)
            {
                // Anchored at the right cheek: near the face, above the shoulder, right of the bow hand.
                Assert.InRange(wrist.Y, RightShoulder.Y, HeadTop - 0.1f);
                Assert.True(wrist.X > grip.X + 0.1f, $"anchor on the draw side of the bow, wrist {wrist}");
            }
        }

        [Fact]
        public void BowHandIsForwardOfTheShoulderAndUnderTheHead()
        {
            var grip = LeftShoulder + (R * RunnerWeaponAim.BowHold.X) + (Up * RunnerWeaponAim.BowHold.Y) + (F * RunnerWeaponAim.BowHold.Z);
            RunnerTwoBoneIk.Solve(LeftShoulder, UpperArm, Forearm, grip, LeftShoulder + RunnerWeaponAim.BowElbowPole(R, Up, F), out var elbow, out var wrist);
            Assert.True(wrist.Z > LeftShoulder.Z + 0.4f, $"bow arm out toward the enemy, wrist {wrist}");
            Assert.InRange(wrist.Y, LeftShoulder.Y - 0.15f, LeftShoulder.Y + 0.15f);
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
