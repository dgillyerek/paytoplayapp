using Grove.Domain.Board;

namespace Grove.Domain.Orders
{
    public enum TeachBeat
    {
        None = 0,
        Crate = 1,
        Merge = 2,
        Deliver = 3,
        Hidden = 4,
        Complete = 5
    }

    /// <summary>
    /// DEV-020 thin teach T1–T3 only. Not full FTUE 0–12.
    /// T1 crate after Front Garden splash → T2 two matching after first spit / 2× WF T1 →
    /// T3 when Order 1 is ready. Skippable after T1 if the player already acted correctly.
    /// </summary>
    public sealed class ThinTeach
    {
        public const float MergeIdleHintSeconds = 4f;
        public const string RingStub = "UI_Teach_Ring";
        public const string HandStub = "UI_Teach_Hand";
        public const string PersistKey = "ftue_play_teach_done";
        public const string LegacyPersistKey = "grove.teach.order1.done";
        public const string RingLabelCrate = "Tap to grow";
        public const string RingLabelMerge = "Stack matches";
        public const string RingLabelDeliver = "Deliver here";

        private readonly PresentationCopy _copy;
        private readonly ITeachSave _save;
        private bool _started;
        private bool _crateTapped;
        private bool _combinedMatches;

        public ThinTeach(PresentationCopy copy, ITeachSave save)
        {
            _copy = copy ?? PresentationCopy.Default;
            _save = save ?? new MemoryTeachSave();
        }

        public TeachBeat Beat { get; private set; }

        public bool Started => _started;

        public bool IsComplete => Beat == TeachBeat.Complete;

        public bool OverlayVisible =>
            Beat == TeachBeat.Crate || Beat == TeachBeat.Merge || Beat == TeachBeat.Deliver;

        public GridPos? HighlightA { get; private set; }

        public GridPos? HighlightB { get; private set; }

        public string Caption =>
            Beat switch
            {
                TeachBeat.Crate => _copy.CoachCrate,
                TeachBeat.Merge => _copy.CoachMerge,
                TeachBeat.Deliver => _copy.CoachDeliver,
                _ => ""
            };

        /// <summary>Design on-ring shorts. Product owns <see cref="Caption"/>.</summary>
        public string RingLabel =>
            Beat switch
            {
                TeachBeat.Crate => RingLabelCrate,
                TeachBeat.Merge => RingLabelMerge,
                TeachBeat.Deliver => RingLabelDeliver,
                _ => ""
            };

        public bool CanSkip(BoardGrid board, OrderBoard orders) =>
            _started && Beat != TeachBeat.Crate && Beat != TeachBeat.None && Beat != TeachBeat.Complete
            && AlreadyActed(board, orders);

        public void StartAfterSplash(BoardGrid board, OrderBoard orders)
        {
            if (_save.IsOrder1TeachDone || orders.CompletedCount > 0)
            {
                Beat = TeachBeat.Complete;
                _started = true;
                ClearHighlights();
                return;
            }

            _started = true;
            Beat = TeachBeat.Crate;
            RefreshHighlights(board);
        }

        public void NotifyCrateTapped(BoardGrid board, OrderBoard orders)
        {
            _crateTapped = true;
            if (!_started || IsComplete)
            {
                return;
            }

            if (Beat == TeachBeat.Crate)
            {
                AdvanceAfterCrate(board, orders);
            }
            else
            {
                Sync(board, orders);
            }
        }

        public void NotifyMatchesCombined(BoardGrid board, OrderBoard orders)
        {
            _combinedMatches = true;
            if (!_started || IsComplete)
            {
                return;
            }

            if (Beat == TeachBeat.Merge)
            {
                Beat = orders.CanDeliver(board) ? TeachBeat.Deliver : TeachBeat.Hidden;
            }

            Sync(board, orders);
        }

        public void NotifyOrderDelivered(OrderBoard orders)
        {
            if (orders.CompletedCount > 0)
            {
                Complete();
            }
        }

        public bool TrySkip(BoardGrid board, OrderBoard orders)
        {
            if (!CanSkip(board, orders))
            {
                return false;
            }

            if (Beat == TeachBeat.Merge)
            {
                Beat = orders.CanDeliver(board) ? TeachBeat.Deliver : TeachBeat.Hidden;
                Sync(board, orders);
                return true;
            }

            if (Beat == TeachBeat.Deliver && orders.CompletedCount > 0)
            {
                Complete();
                return true;
            }

            if (Beat == TeachBeat.Hidden)
            {
                Sync(board, orders);
                return true;
            }

            return false;
        }

        public void Sync(BoardGrid board, OrderBoard orders)
        {
            if (!_started || IsComplete)
            {
                return;
            }

            if (orders.CompletedCount > 0)
            {
                Complete();
                return;
            }

            if (Beat == TeachBeat.Crate)
            {
                RefreshHighlights(board);
                return;
            }

            if (Beat == TeachBeat.Hidden || Beat == TeachBeat.Merge)
            {
                if (orders.CanDeliver(board) && (_combinedMatches || Beat == TeachBeat.Hidden))
                {
                    Beat = TeachBeat.Deliver;
                }
                else if (Beat == TeachBeat.Hidden && board.TryFindTwoMatching(out _, out _) && !_combinedMatches)
                {
                    Beat = TeachBeat.Merge;
                }
                else if (Beat == TeachBeat.Merge && !board.TryFindTwoMatching(out _, out _) && !orders.CanDeliver(board))
                {
                    Beat = TeachBeat.Hidden;
                }
            }

            if (Beat == TeachBeat.Hidden && orders.CanDeliver(board))
            {
                Beat = TeachBeat.Deliver;
            }

            RefreshHighlights(board);
        }

        private void AdvanceAfterCrate(BoardGrid board, OrderBoard orders)
        {
            if (orders.CanDeliver(board))
            {
                Beat = TeachBeat.Deliver;
            }
            else if (board.TryFindTwoMatching(out _, out _))
            {
                Beat = TeachBeat.Merge;
            }
            else
            {
                Beat = TeachBeat.Hidden;
            }

            RefreshHighlights(board);
        }

        private bool AlreadyActed(BoardGrid board, OrderBoard orders)
        {
            if (Beat == TeachBeat.Merge)
            {
                return _combinedMatches || orders.CanDeliver(board);
            }

            if (Beat == TeachBeat.Deliver)
            {
                return orders.CompletedCount > 0;
            }

            return _crateTapped && Beat != TeachBeat.Crate;
        }

        private void RefreshHighlights(BoardGrid board)
        {
            HighlightA = null;
            HighlightB = null;
            if (Beat == TeachBeat.Merge && board.TryFindTwoMatching(out var a, out var b))
            {
                HighlightA = a;
                HighlightB = b;
            }
        }

        private void ClearHighlights()
        {
            HighlightA = null;
            HighlightB = null;
        }

        private void Complete()
        {
            Beat = TeachBeat.Complete;
            _save.IsOrder1TeachDone = true;
            ClearHighlights();
        }
    }
}
