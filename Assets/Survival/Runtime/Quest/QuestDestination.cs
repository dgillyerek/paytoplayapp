#nullable enable
using System;
using Survival.Domain.Ids;

namespace Survival.Domain.Quest
{
    /// <summary>Where tapping the town quest bar sends the player.</summary>
    public enum QuestGoKind
    {
        /// <summary>Nothing to do (chain finished / unknown objective).</summary>
        None,

        /// <summary>The step is done: claim its chest.</summary>
        Claim,

        /// <summary>Pan the town to a building (by building id) and open it.</summary>
        Building,

        /// <summary>Switch to the world map and open a world node (by node id).</summary>
        WorldNode,
    }

    public sealed record QuestGo(QuestGoKind Kind, string Target);

    public static class QuestDestination
    {
        /// <summary>Route for the quest bar: claim when done, otherwise where the objective is worked.</summary>
        public static QuestGo For(QuestStepDef? step, bool complete)
        {
            if (step == null)
            {
                return new QuestGo(QuestGoKind.None, "");
            }

            if (complete)
            {
                return new QuestGo(QuestGoKind.Claim, step.Id);
            }

            return step.Objective switch
            {
                SurvIds.QuestObjectiveCollect => new QuestGo(QuestGoKind.Building, step.Target),
                SurvIds.QuestObjectiveReachLevel => new QuestGo(QuestGoKind.Building, step.Target),
                SurvIds.QuestObjectiveGather => new QuestGo(QuestGoKind.WorldNode, step.Target),
                _ => new QuestGo(QuestGoKind.None, step.Target),
            };
        }

        public static QuestGo For(QuestSystem quests) => For(quests.Current, quests.IsComplete);
    }
}
