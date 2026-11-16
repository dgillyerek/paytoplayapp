using Survival.Domain.Kids;

namespace Survival.Domain.Tests;

public sealed class KidsChatFilterTests
{
    private static ChatFilter Shipped() =>
        ChatFilter.Parse(File.ReadAllText(Path.Combine(KidsFiles.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", "kids", ChatFilter.FileName)));

    [Fact]
    public void Shipped_list_loads_with_links_and_contact_info_blocked()
    {
        var f = Shipped();
        Assert.True(f.WordCount > 20);
        Assert.True(f.BlockLinks);
        Assert.True(f.BlockContactInfo);
    }

    [Theory]
    [InlineData("what the fuck", "what the ****")]
    [InlineData("SHIT happens", "**** happens")]
    [InlineData("you b1tch", "you *****")]
    [InlineData("fuuuuck", "*******")]
    [InlineData("sh1t and $hit", "**** and ****")]
    public void Profanity_is_masked_whole_word_after_folding(string body, string expected)
    {
        var r = Shipped().Apply(body);
        Assert.Equal(expected, r.Text);
        Assert.Null(r.Refusal);
        Assert.True(r.Flagged);
    }

    [Theory]
    [InlineData("classic assassin from Scunthorpe")]
    [InlineData("Shitake mushrooms? no, shiitake")]
    [InlineData("I have 1500000 gold and level 31")]
    [InlineData("good game, see you at the castle")]
    public void Clean_text_passes_untouched(string body)
    {
        var r = Shipped().Apply(body);
        Assert.Equal(body, r.Text);
        Assert.False(r.Flagged);
    }

    [Theory]
    [InlineData("join my discord https://discord.gg/abc", "links_not_allowed")]
    [InlineData("go to www.example.org", "links_not_allowed")]
    [InlineData("free gems at gems4u.xyz", "links_not_allowed")]
    [InlineData("mail me at kid@example.com", "links_not_allowed")]
    [InlineData("text me 0917 555 1234", "personal_info")]
    [InlineData("call +1 (416) 555-0199", "personal_info")]
    public void Links_and_contact_info_are_refused(string body, string refusal) =>
        Assert.Equal(refusal, Shipped().Apply(body).Refusal);

    [Fact]
    public void Free_chat_display_is_masked_and_presets_mode_still_hides_free_text()
    {
        var before = ChatSafety.Filter;
        try
        {
            ChatSafety.Filter = Shipped();
            var adult = KidsFiles.Policy(KidsFiles.Shipped(), "13plus");
            Assert.Equal("well **** that", ChatSafety.Visible(adult, 7, "well shit that", new HashSet<long>(), _ => null));
            var child = KidsFiles.FamiliesPolicy(KidsFiles.Shipped(), "under13");
            Assert.Null(ChatSafety.Visible(new ChildSafetyPolicy(child.Config, child.State, ChatMode.Presets), 7, "well shit that", new HashSet<long>(), _ => null));
        }
        finally
        {
            ChatSafety.Filter = before;
        }
    }
}
