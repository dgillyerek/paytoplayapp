using System.Collections.Generic;

namespace Grove.Domain.Orders
{
    public enum CoachAdvance
    {
        Skip = 0,
        Crate = 1,
        Merge = 2,
        Deliver = 3
    }

    /// <summary>One first-run coach mark (crate → merge → deliver). Not full FTUE 0–12.</summary>
    public sealed record CoachMark(CoachAdvance AdvanceOn, string Caption, string ArtStub);

    /// <summary>
    /// Minimal Play Mode guidance after DEV-018 splashes. Advances on the matching verb
    /// (crate tap / merge / deliver) or an explicit skip.
    /// </summary>
    public sealed class FirstRunCoach
    {
        private readonly IReadOnlyList<CoachMark> _marks;
        private int _index;

        public FirstRunCoach(PresentationCopy copy)
        {
            _marks = CreateMarks(copy ?? PresentationCopy.Default);
        }

        public bool Started { get; private set; }

        public bool IsComplete => _index >= _marks.Count;

        public CoachMark? Current => IsComplete ? null : _marks[_index];

        public int StepIndex => _index;

        public void Start() => Started = true;

        public static IReadOnlyList<CoachMark> CreateMarks(PresentationCopy copy) =>
            new CoachMark[]
            {
                new CoachMark(CoachAdvance.Crate, copy.CoachCrate, "ENV_FG_GardenCrate_Charged"),
                new CoachMark(CoachAdvance.Merge, copy.CoachMerge, "VFX_MergeSparkle"),
                new CoachMark(CoachAdvance.Deliver, copy.CoachDeliver, "UI_OrderTray_Card")
            };

        public bool TryAdvance(CoachAdvance reason)
        {
            if (!Started || IsComplete)
            {
                return false;
            }

            var current = _marks[_index];
            if (reason != CoachAdvance.Skip && reason != current.AdvanceOn)
            {
                return false;
            }

            _index++;
            return true;
        }
    }
}
