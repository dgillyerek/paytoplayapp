using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests
{
    public sealed class RunnerCombatStanceTests
    {
        [Fact]
        public void InRangeAttacksElseWalks()
        {
            Assert.Equal(RunnerStance.Attack, RunnerCombatStance.For(true));
            Assert.Equal(RunnerStance.Walk, RunnerCombatStance.For(false));
        }

        [Fact]
        public void StartsWalkingWithNoAim()
        {
            var s = new RunnerCombatStance();
            Assert.Equal(RunnerStance.Walk, s.Step(false, 0.1f));
            Assert.Equal(0f, s.Weight);
        }

        [Fact]
        public void TargetInRangeAttacksAtOnceAndBlendsIn()
        {
            var s = new RunnerCombatStance();
            Assert.Equal(RunnerStance.Attack, s.Step(true, 0.1f));
            Assert.InRange(s.Weight, 0.01f, 0.99f);
            for (var i = 0; i < 5; i++)
            {
                s.Step(true, 0.1f);
            }

            Assert.Equal(1f, s.Weight);
        }

        [Fact]
        public void FlickerAtTheRangeEdgeHoldsTheAttack()
        {
            var s = new RunnerCombatStance();
            for (var i = 0; i < 40; i++)
            {
                Assert.Equal(RunnerStance.Attack, s.Step(i % 3 != 2, 0.05f));
            }

            Assert.Equal(1f, s.Weight);
        }

        [Fact]
        public void NothingInRangeDropsToTheWalkAfterTheHold()
        {
            var s = new RunnerCombatStance();
            for (var i = 0; i < 10; i++)
            {
                s.Step(true, 0.1f);
            }

            Assert.Equal(RunnerStance.Attack, s.Step(false, RunnerCombatStance.HoldSeconds * 0.5f));
            s.Step(false, RunnerCombatStance.HoldSeconds);
            Assert.Equal(RunnerStance.Walk, s.Stance);
            for (var i = 0; i < 5; i++)
            {
                s.Step(false, 0.1f);
            }

            Assert.Equal(0f, s.Weight);
        }

        [Fact]
        public void ResetWalks()
        {
            var s = new RunnerCombatStance();
            s.Step(true, 1f);
            s.Reset();
            Assert.Equal(RunnerStance.Walk, s.Stance);
            Assert.Equal(0f, s.Weight);
        }
    }
}
