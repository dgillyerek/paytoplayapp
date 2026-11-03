using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Ids;
using Survival.Domain.Quest;

namespace Survival.Domain.Tests;

public class QuestDestinationTests
{
    private static QuestStepDef Step(string objective, string target) =>
        new("q", objective, target, 1, new Dictionary<string, int>());

    [Theory]
    [InlineData(SurvIds.QuestObjectiveCollect, "building.economy", QuestGoKind.Building)]
    [InlineData(SurvIds.QuestObjectiveReachLevel, "building.hq", QuestGoKind.Building)]
    [InlineData(SurvIds.QuestObjectiveGather, "world.node.gather_01", QuestGoKind.WorldNode)]
    [InlineData("quest.objective.unknown", "x", QuestGoKind.None)]
    public void EachObjectiveGoesWhereItIsDone(string objective, string target, QuestGoKind kind)
    {
        var go = QuestDestination.For(Step(objective, target), complete: false);
        Assert.Equal(kind, go.Kind);
        Assert.Equal(target, go.Target);
    }

    [Fact]
    public void CompleteStepClaims() =>
        Assert.Equal(QuestGoKind.Claim, QuestDestination.For(Step(SurvIds.QuestObjectiveGather, "n"), complete: true).Kind);

    [Fact]
    public void FinishedChainGoesNowhere() =>
        Assert.Equal(QuestGoKind.None, QuestDestination.For(null, complete: false).Kind);

    [Fact]
    public void EveryShippedStepHasAPlaceToGo()
    {
        var c = Hook.Catalog();
        foreach (var step in c.StarterChain.Steps)
        {
            var go = QuestDestination.For(step, complete: false);
            Assert.NotEqual(QuestGoKind.None, go.Kind);
            if (go.Kind == QuestGoKind.Building)
            {
                Assert.Contains(c.Plots, p => p.BuildingId == go.Target);
            }
        }
    }
}
