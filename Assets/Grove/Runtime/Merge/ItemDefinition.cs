using Grove.Domain.Board;

namespace Grove.Domain.Merge
{
    /// <summary>One catalog row. Tiers and display names live in JSON; merge logic never hardcodes chain length.</summary>
    public sealed record ItemDefinition(
        PieceId Id,
        string DisplayName,
        string Chain,
        int Tier,
        bool Mergeable);
}
