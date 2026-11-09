using Survival.Domain.Kids;

namespace Survival.Domain.Tests;

public sealed class KidsAgeGateTests
{
    private static readonly DateTime Today = new(2026, 11, 9);
    private static readonly ChildSafetyConfig Config = ChildSafetyConfig.Default;

    [Theory]
    [InlineData(2013, 11, 13)] // 13th birthday this month
    [InlineData(2013, 12, 12)] // turns 13 next month
    [InlineData(2014, 1, 12)]
    [InlineData(1990, 6, 36)]
    public void Age_counts_whole_years_from_the_birth_month(int year, int month, int age) =>
        Assert.Equal(age, AgeGate.AgeInYears(year, month, Today));

    [Fact]
    public void Twelve_is_a_child_band_and_thirteen_is_not()
    {
        Assert.Equal("under13", AgeGate.Classify(2013, 12, Today, Config).Id);
        Assert.True(AgeGate.Classify(2013, 12, Today, Config).Child);
        Assert.Equal("13plus", AgeGate.Classify(2013, 11, Today, Config).Id);
        Assert.False(AgeGate.Classify(2013, 11, Today, Config).Child);
    }

    [Fact]
    public void Missing_future_or_implausible_answers_are_rejected_not_guessed()
    {
        Assert.Equal(AgeEntryError.MissingYear, AgeGate.Validate(null, 3, Today, Config));
        Assert.Equal(AgeEntryError.MissingMonth, AgeGate.Validate(2000, null, Today, Config));
        Assert.Equal(AgeEntryError.YearOutOfRange, AgeGate.Validate(2027, 1, Today, Config));
        Assert.Equal(AgeEntryError.YearOutOfRange, AgeGate.Validate(1900, 1, Today, Config));
        Assert.Equal(AgeEntryError.InFuture, AgeGate.Validate(2026, 12, Today, Config));
        Assert.Throws<ArgumentOutOfRangeException>(() => AgeGate.Classify(2027, 1, Today, Config));
    }

    [Fact]
    public void Configured_bands_split_children_further()
    {
        var json = "{\"age_bands\":[{\"id\":\"6to8\",\"max_age\":8,\"child\":true},{\"id\":\"9to12\",\"max_age\":12,\"child\":true},{\"id\":\"13plus\",\"child\":false}]}";
        var c = ChildSafetyConfig.Parse(json);
        Assert.Equal("6to8", AgeGate.Classify(2019, 1, Today, c).Id);
        Assert.Equal("9to12", AgeGate.Classify(2015, 1, Today, c).Id);
        Assert.Equal("13plus", AgeGate.Classify(2000, 1, Today, c).Id);
    }

    [Fact]
    public void Bands_must_rise_and_end_open()
    {
        Assert.Throws<FormatException>(() => ChildSafetyConfig.Parse("{\"age_bands\":[{\"id\":\"a\",\"max_age\":12},{\"id\":\"b\",\"max_age\":8},{\"id\":\"c\"}]}"));
        Assert.Throws<FormatException>(() => ChildSafetyConfig.Parse("{\"age_bands\":[{\"id\":\"a\",\"max_age\":12}]}"));
        Assert.Throws<FormatException>(() => ChildSafetyConfig.Parse("{\"age_bands\":[{\"id\":\"a\"},{\"id\":\"b\"}]}"));
    }

    [Fact]
    public void A_fresh_install_asks_and_an_answer_is_kept_and_final()
    {
        var storage = new MemoryKidStorage();
        var store = new AgeGateStore(storage, () => Today);
        Assert.Equal(AgeGateStatus.Fresh, store.Load().Status);
        Assert.True(store.Load().NeedsGate);

        store.MarkShown();
        Assert.True(store.Save(Config.Band("under13")!));
        var state = store.Load();
        Assert.Equal(AgeGateStatus.Answered, state.Status);
        Assert.Equal("under13", state.BandId);
        Assert.False(state.NeedsGate);

        // Asking again cannot change it.
        Assert.False(store.Save(Config.Band("13plus")!));
        Assert.Equal("under13", new AgeGateStore(storage).Load().BandId);
    }

    [Fact]
    public void Only_the_band_is_stored_never_the_birth_date()
    {
        var storage = new MemoryKidStorage();
        var store = new AgeGateStore(storage, () => Today);
        store.MarkShown();
        store.Save(AgeGate.Classify(2014, 7, Today, Config));
        Assert.DoesNotContain(storage.Values.Values, v => v.Contains("2014") || v.Contains(";7;"));
    }

    [Fact]
    public void Closing_the_app_mid_question_asks_again()
    {
        var storage = new MemoryKidStorage();
        new AgeGateStore(storage).MarkShown();
        var state = new AgeGateStore(storage).Load();
        Assert.Equal(AgeGateStatus.Interrupted, state.Status);
        Assert.True(state.NeedsGate);
    }

    [Fact]
    public void Editing_the_stored_band_locks_child_mode_and_does_not_re_ask()
    {
        var storage = new MemoryKidStorage();
        var store = new AgeGateStore(storage, () => Today);
        store.MarkShown();
        store.Save(Config.Band("under13")!);
        storage.Values[AgeGateStore.RecordKey] = storage.Values[AgeGateStore.RecordKey].Replace("under13", "13plus");

        var state = store.Load();
        Assert.Equal(AgeGateStatus.Tampered, state.Status);
        Assert.False(state.NeedsGate);
        Assert.True(new ChildSafetyPolicy(Config, state).IsChild);
        Assert.False(store.Save(Config.Band("13plus")!));
    }

    [Fact]
    public void Deleting_the_stored_answer_locks_child_mode()
    {
        var storage = new MemoryKidStorage();
        var store = new AgeGateStore(storage, () => Today);
        store.MarkShown();
        store.Save(Config.Band("13plus")!);
        storage.Values.Remove(AgeGateStore.RecordKey);
        Assert.Equal(AgeGateStatus.Tampered, store.Load().Status);
        Assert.True(new ChildSafetyPolicy(Config, store.Load()).IsChild);
    }

    [Fact]
    public void A_forged_pending_mark_is_tampering_too()
    {
        var storage = new MemoryKidStorage();
        var store = new AgeGateStore(storage, () => Today);
        store.MarkShown();
        storage.Values[AgeGateStore.PendingKey] = "forged";
        Assert.Equal(AgeGateStatus.Tampered, store.Load().Status);
    }

    [Fact]
    public void Account_deletion_clears_everything_so_the_next_launch_asks()
    {
        var storage = new MemoryKidStorage();
        var store = new AgeGateStore(storage, () => Today);
        store.MarkShown();
        store.Save(Config.Band("13plus")!);
        store.Clear();
        Assert.Empty(storage.Values);
        Assert.Equal(AgeGateStatus.Fresh, store.Load().Status);
    }
}
