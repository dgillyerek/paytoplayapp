#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Economy;
using Survival.Domain.Ids;

namespace Survival.Domain.Quest
{
    public sealed record QuestStepDef(
        string Id,
        string Objective,
        string Target,
        int Count,
        IReadOnlyDictionary<string, int> Rewards);

    public sealed record QuestChainDef(string Id, IReadOnlyList<QuestStepDef> Steps);

    /// <summary>Linear quest chain. Only the active step listens; finish → claim chest → next step.</summary>
    public sealed class QuestSystem
    {
        private readonly Func<string, int>? _levelOf;

        public QuestSystem(QuestChainDef chain, Func<string, int>? levelOf = null)
        {
            Chain = chain ?? throw new ArgumentNullException(nameof(chain));
            _levelOf = levelOf;
            SeedProgress();
        }

        public QuestChainDef Chain { get; }
        public int Index { get; private set; }
        public int Progress { get; private set; }
        public bool ChainDone => Index >= Chain.Steps.Count;
        public QuestStepDef? Current => ChainDone ? null : Chain.Steps[Index];
        public bool IsComplete => Current != null && Progress >= Current.Count;

        /// <summary>Fired when the active step first reaches its count (ready to claim).</summary>
        public event Action<QuestStepDef>? StepCompleted;

        public void Report(string objective, string target, int amount)
        {
            var step = Current;
            if (step == null || IsComplete || amount <= 0)
            {
                return;
            }

            if (!string.Equals(step.Objective, objective, StringComparison.Ordinal)
                || !string.Equals(step.Target, target, StringComparison.Ordinal))
            {
                return;
            }

            if (string.Equals(objective, SurvIds.QuestObjectiveReachLevel, StringComparison.Ordinal))
            {
                Progress = Math.Min(step.Count, Math.Max(Progress, amount));
            }
            else
            {
                Progress = Math.Min(step.Count, Progress + amount);
            }

            if (IsComplete)
            {
                StepCompleted?.Invoke(step);
            }
        }

        public void ReportLevel(string buildingId, int level) =>
            Report(SurvIds.QuestObjectiveReachLevel, buildingId, level);

        /// <summary>Grant the active step's rewards and advance. Returns the rewards, or null when not claimable.</summary>
        public IReadOnlyDictionary<string, int>? Claim(Wallet wallet)
        {
            var step = Current;
            if (step == null || !IsComplete)
            {
                return null;
            }

            wallet.Grant(step.Rewards);
            Index++;
            Progress = 0;
            SeedProgress();
            return step.Rewards;
        }

        /// <summary>A reach-level step that is already met starts complete.</summary>
        private void SeedProgress()
        {
            var step = Current;
            if (step == null || _levelOf == null)
            {
                return;
            }

            if (string.Equals(step.Objective, SurvIds.QuestObjectiveReachLevel, StringComparison.Ordinal))
            {
                Progress = Math.Min(step.Count, _levelOf(step.Target));
            }
        }
    }
}
