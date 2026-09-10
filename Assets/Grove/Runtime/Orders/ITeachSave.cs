namespace Grove.Domain.Orders
{
    /// <summary>Persists DEV-020 thin teach once per save (<c>ftue_play_teach_done</c>) until Order 1 completes.</summary>
    public interface ITeachSave
    {
        bool IsOrder1TeachDone { get; set; }
    }

    public sealed class MemoryTeachSave : ITeachSave
    {
        public bool IsOrder1TeachDone { get; set; }
    }
}
