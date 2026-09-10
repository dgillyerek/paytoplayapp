using System;
using Grove.Domain.Board;
using Grove.Domain.Commerce;
using Grove.Domain.Energy;
using Grove.Domain.Merge;
using Grove.Domain.Orders;
using Grove.Domain.Producer;
using Grove.Domain.Time;
using UnityEngine;

namespace Grove.Unity
{
    /// <summary>Playable prototype: 7×5 drag-merge, garden crate, energy HUD, scripted orders 1–6.</summary>
    public sealed class GroveBootstrap : MonoBehaviour
    {
        [SerializeField] private BoardView boardView = null!;
        [SerializeField] private DragMergeController dragController = null!;
        [SerializeField] private GroveCatalogAsset catalogAsset = null!;

        public MergeSession Session { get; private set; } = null!;
        public FakeStore Store { get; private set; } = null!;
        public LoadedCatalog Catalog { get; private set; } = null!;
        public EnergyWallet Energy { get; private set; } = null!;
        public GardenCrate Crate { get; private set; } = null!;
        public CrateTapService CrateTap { get; private set; } = null!;
        public OrderBoard Orders { get; private set; } = null!;
        public SplashStub Splash { get; private set; } = null!;
        public IClock Clock { get; private set; } = null!;
        public PrototypeHud Hud { get; private set; } = null!;

        public BoardView BoardView => boardView;
        public DragMergeController Drag => dragController;

        private bool _booted;

        private void Awake() => TryBoot();

        private void OnEnable() => TryBoot();

        private void Start() => TryBoot();

        private void Update()
        {
            if (!_booted)
            {
                TryBoot();
            }
        }

        private void TryBoot()
        {
            GroveVisuals.EnsurePlayCamera();
            if (_booted || !Application.isPlaying)
            {
                return;
            }

            try
            {
                Boot();
                _booted = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                GroveVisuals.ShowDiagnostic("Grove bootstrap failed (not an empty-blue camera).\n" + ex.Message);
                _booted = true;
            }
        }

        private void Boot()
        {
            if (boardView == null)
            {
                boardView = GetComponent<BoardView>();
            }

            if (dragController == null)
            {
                dragController = GetComponent<DragMergeController>();
            }

            Clock = new SystemClock();
            Catalog = catalogAsset != null ? catalogAsset.Load() : LoadCatalog();
            var board = new BoardGrid();
            Session = new MergeSession(board, Catalog.Recipes);
            Store = new FakeStore();
            Hud = GetComponent<PrototypeHud>() ?? gameObject.AddComponent<PrototypeHud>();
            Energy = new EnergyWallet(Catalog.Energy, Clock, Hud);
            if (Catalog.GardenCrate != null)
            {
                Crate = new GardenCrate(Catalog.GardenCrate, new RandomAdapter(), ftueFreeTapRemaining: true);
                CrateTap = new CrateTapService(Crate, board, Energy, Clock);
            }

            Orders = new OrderBoard(
                Catalog.Orders.Count > 0 ? Catalog.Orders : GroveCatalog.CreateFallback().Orders,
                Catalog.Copy.OrderSlotsMax);
            Splash = new SplashStub(Catalog.Copy);
            SeedDemoBoard(board);

            if (boardView != null)
            {
                boardView.Bind(board);
            }

            if (dragController != null)
            {
                dragController.Bind(Session, boardView);
            }

            Hud.Host = this;
            Hud.Build();
            Hud.Toast("QA: CRATE → merge to Sprout (WF T2) → DELIVER Order 1.");
        }

        internal static LoadedCatalog LoadCatalog()
        {
            try
            {
                var items = Resources.Load<TextAsset>("Grove/items");
                var recipes = Resources.Load<TextAsset>("Grove/recipes");
                if (items != null && recipes != null)
                {
                    return CatalogLoader.FromJson(
                        items.text,
                        recipes.text,
                        Resources.Load<TextAsset>("Grove/garden_crate")?.text,
                        Resources.Load<TextAsset>("Grove/energy")?.text,
                        Resources.Load<TextAsset>("Grove/orders")?.text,
                        Resources.Load<TextAsset>("Grove/copy")?.text);
                }
            }
            catch (Exception)
            {
                // Fall through to in-memory catalog so Play Mode never hard-fails.
            }

            return GroveCatalog.CreateFallback();
        }

        /// <summary>One starter wildflower so the board is not empty; crate is the producer.</summary>
        internal static void SeedDemoBoard(BoardGrid board)
        {
            board.Place(new GridPos(3, 2), new PieceStack(GroveCatalog.WildflowerT1, 1));
        }
    }
}
