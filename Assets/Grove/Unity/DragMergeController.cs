using System.Collections;
using Grove.Domain.Board;
using Grove.Domain.Merge;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Grove.Unity
{
    /// <summary>
    /// Mouse / touch drag → <see cref="MergeSession.TryDrag"/>.
    /// Pieces follow the pointer; illegal drops lerp back; merges punch the destination.
    /// </summary>
    public sealed class DragMergeController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera = null!;
        [SerializeField] private float snapBackSeconds = 0.18f;

        private bool _dragging;
        private bool _busy;
        private GridPos _from;
        private PieceStack _stack;

        public MergeSession? Session { get; private set; }
        public BoardView? View { get; private set; }
        public bool IsDragging => _dragging;
        public bool IsBusy => _busy;
        public DragResult? LastResult { get; private set; }
        public string LastFeedback { get; private set; } = "";

        public void Bind(MergeSession session, BoardView view)
        {
            Session = session;
            View = view;
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
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

        private void Update()
        {
            if (Session == null || View == null || _busy)
            {
                return;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null)
            {
                return;
            }

            if (!_dragging && Pressed() && !PointerOverUi())
            {
                TryBeginDrag();
            }
            else if (_dragging && Held())
            {
                var world = PointerWorld();
                View.MoveGhost(world);
                View.SetHover(View.TryWorldToCell(world, out var hover) ? hover : null);
            }
            else if (_dragging && Released())
            {
                StartCoroutine(EndDrag());
            }
        }

        private void TryBeginDrag()
        {
            var world = PointerWorld();
            if (!View!.TryWorldToCell(world, out var cell) || Session!.Board.TryGet(cell, out var stack) == false)
            {
                return;
            }

            _dragging = true;
            _from = cell;
            _stack = stack;
            View.HiddenCell = cell;
            View.Refresh();
            View.ShowGhost(stack, world);
            LastFeedback = "Drag onto a match (3 → next tier) or an empty cell.";
        }

        private IEnumerator EndDrag()
        {
            _dragging = false;
            _busy = true;
            var world = PointerWorld();
            View!.SetHover(null);
            var hasCell = View.TryWorldToCell(world, out var to);
            var result = hasCell
                ? Drop(_from, to)
                : new DragResult.SnapBack("out-of-bounds");
            LastResult = result;

            if (result is DragResult.SnapBack snap)
            {
                LastFeedback = SnapMessage(snap.Reason);
                var fromWorld = PointerWorld();
                var dest = View.CellToWorld(_from);
                var t = 0f;
                while (t < snapBackSeconds)
                {
                    t += Time.deltaTime;
                    var u = Mathf.Clamp01(t / snapBackSeconds);
                    u = 1f - (1f - u) * (1f - u);
                    View.MoveGhost(Vector3.Lerp(fromWorld, dest, u));
                    yield return null;
                }
            }
            else if (result is DragResult.Applied applied)
            {
                var dest = View.CellToWorld(hasCell ? to : _from);
                var fromWorld = PointerWorld();
                var t = 0f;
                var fly = 0.08f;
                while (t < fly)
                {
                    t += Time.deltaTime;
                    View.MoveGhost(Vector3.Lerp(fromWorld, dest, Mathf.Clamp01(t / fly)));
                    yield return null;
                }

                LastFeedback = applied.Merge is { } merge
                    ? $"Merged {merge.Consumed} → {merge.Produced}!"
                    : "Moved.";
            }

            View.HideGhost();
            View.HiddenCell = null;
            View.Refresh();
            if (result is DragResult.Applied { Merge: not null } merged)
            {
                View.Punch(merged.Merge!.At);
            }

            _busy = false;
        }

        private static string SnapMessage(string reason) => reason switch
        {
            "type-mismatch" => "Snap back — those pieces don't match.",
            "no-recipe" => "Snap back — this tier doesn't merge further.",
            "overstack" => "Snap back — too many on that cell.",
            "same-cell" => "Snap back — drop on another cell.",
            "empty-source" => "Snap back — nothing to drag.",
            "out-of-bounds" => "Snap back — drop on the board.",
            _ => "Snap back — " + reason
        };

        private Vector3 PointerWorld()
        {
            var screen = (Vector3)(Vector2)Input.mousePosition;
            var world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
            world.z = 0f;
            return world;
        }

        private static bool Pressed() => Input.GetMouseButtonDown(0);

        private static bool Held() => Input.GetMouseButton(0);

        private static bool Released() => Input.GetMouseButtonUp(0);

        private static bool PointerOverUi() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
