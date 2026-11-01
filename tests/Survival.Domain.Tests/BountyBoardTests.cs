using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Taps;
using Xunit;

public class BountyBoardTests
{
    private const double T0 = 1_800_000_000;

    private static (BountyBoard board, double[] now) Make(int seed = 3)
    {
        var now = new[] { T0 };
        return (new BountyBoard(BountyConfig.Default, () => now[0], seed), now);
    }

    [Fact]
    public void StartsWithFourOfferedPosters_Deterministic()
    {
        var (a, _) = Make(11);
        var (b, _) = Make(11);
        Assert.Equal(4, a.Slots.Count);
        Assert.All(a.Slots, s => Assert.Equal(BountyState.Offered, s.StateAt(T0)));
        Assert.Equal(a.Slots.Select(s => (s.Kind, s.Target, s.Giver)), b.Slots.Select(s => (s.Kind, s.Target, s.Giver)));
    }

    [Fact]
    public void AcceptLimit_TwoActiveAtOnce()
    {
        var (board, _) = Make();
        Assert.Equal(BountyResult.Ok, board.Accept(0));
        Assert.Equal(BountyResult.WrongState, board.Accept(0));
        Assert.Equal(BountyResult.Ok, board.Accept(1));
        Assert.Equal(BountyResult.TooManyActive, board.Accept(2));
        Assert.Equal(BountyResult.NoSuchBounty, board.Accept(9));
    }

    [Fact]
    public void Progress_Claim_PaysEconomyOnly()
    {
        var allowed = new HashSet<string> { SurvIds.ResSoft, SurvIds.ResWood, SurvIds.ResStone, SurvIds.ResFood, SurvIds.ResSpeedupM };
        for (var seed = 0; seed < 40; seed++)
        {
            var (board, _) = Make(seed);
            board.Accept(0);
            var b = board.Slots[0];
            Assert.Null(board.Claim(0));
            board.Report("nope", 999);
            Assert.Equal(0, b.Progress);
            Assert.Equal(1, board.Report(b.Kind, b.Target));
            Assert.Equal(1, board.ReadyCount);
            var bag = board.Claim(0)!;
            Assert.NotEmpty(bag);
            Assert.All(bag.Keys, k => Assert.Contains(k, allowed));
            Assert.Equal(BountyState.Claimed, b.StateAt(board.Now));
            Assert.Null(board.Claim(0));
        }
    }

    [Fact]
    public void ActiveBountyExpires_AfterItsLimit()
    {
        var (board, now) = Make();
        board.Accept(0);
        now[0] += board.Slots[0].LimitSeconds;
        Assert.Equal(BountyState.Expired, board.Slots[0].StateAt(now[0]));
        Assert.Equal(0, board.Report(board.Slots[0].Kind, 999));
    }

    [Fact]
    public void Timer_RepostsAllButInProgress()
    {
        var (board, now) = Make();
        board.Accept(0);
        var kept = board.Slots[0];
        board.Report(kept.Kind, kept.Target); // finished but unclaimed posters stay on the board
        var old1 = board.Slots[1];
        now[0] += BountyConfig.Default.RefreshEverySeconds - 1;
        Assert.False(board.Tick());
        now[0] += 1;
        Assert.True(board.Tick());
        Assert.Same(kept, board.Slots[0]);
        Assert.NotSame(old1, board.Slots[1]);
    }

    [Fact]
    public void BuyRefresh_CostsGems()
    {
        var (board, _) = Make();
        var w = new Wallet(new Dictionary<string, int> { [SurvIds.ResHard] = 30 });
        var old = board.Slots[2];
        Assert.Equal(BountyResult.Ok, board.BuyRefresh(w));
        Assert.Equal(5, w.Get(SurvIds.ResHard));
        Assert.NotSame(old, board.Slots[2]);
        Assert.Equal(BountyResult.NotEnoughGems, board.BuyRefresh(w));
    }

    [Fact]
    public void SaveRoundTrip()
    {
        var (board, now) = Make();
        board.Accept(1);
        board.Report(board.Slots[1].Kind, 1);
        var copy = new BountyBoard(BountyConfig.Default, () => now[0], 99);
        Assert.True(copy.Apply(board.Capture()));
        for (var i = 0; i < 4; i++)
        {
            var a = board.Slots[i];
            var c = copy.Slots[i];
            Assert.Equal((a.Kind, a.Target, a.Progress, a.Accepted, a.Rare, a.Giver), (c.Kind, c.Target, c.Progress, c.Accepted, c.Rare, c.Giver));
            Assert.Equal(a.Reward, c.Reward);
            Assert.Equal(a.EndsAt, c.EndsAt);
        }

        Assert.False(copy.Apply("{oops"));
    }
}
