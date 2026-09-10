using Grove.Domain.Board;
using Grove.Domain.Merge;
using UnityEngine;

namespace Grove.Unity
{
    /// <summary>
    /// Pointer drag → <see cref="MergeSession.TryDrag"/>. Snap-back means the domain rejected the drop
    /// and the view must return the piece to its origin (no board mutation).
    /// </summary>
    public sealed class DragMergeController : MonoBehaviour
    {
        public MergeSession? Session { get; private set; }
        public BoardView? View { get; private set; }

        public DragResult? LastResult { get; private set; }

        public void Bind(MergeSession session, BoardView view)
        {
            Session = session;
            View = view;
        }

        public DragResult Drop(GridPos from, GridPos to)
        {
            if (Session == null)
            {
                LastResult = new DragResult.SnapBack("unbound");
                return LastResult;
            }

            LastResult = Session.TryDrag(from, to);
            return LastResult;
        }
    }
}
