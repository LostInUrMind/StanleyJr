using Stanley.Bot.Presentation;

namespace Stanley.Bot.Tests;

/// <summary>
/// The wording FR-09 and FR-10 state word for word, read from the one place that holds it
/// rather than copied into the test.
/// </summary>
public class RepliesTests
{
    [Fact]
    public void A_fighter_with_no_aliases_is_said_to_have_none()
    {
        Assert.Equal("Heat Synced has no aliases.", Replies.AliasList("Heat Synced", []));
    }

    [Theory]
    // One alias, two joined by "and", and three with the serial comma FR-09 uses.
    [InlineData("Heat Synced is also known as heat.", "heat")]
    [InlineData("Heat Synced is also known as heat and hs.", "heat", "hs")]
    [InlineData("Heat Synced is also known as heat, hs, and synced.", "heat", "hs", "synced")]
    public void A_fighters_aliases_are_read_out_as_a_sentence(string expected, params string[] aliases)
    {
        Assert.Equal(expected, Replies.AliasList("Heat Synced", aliases));
    }

    [Fact]
    public void A_refused_alias_names_the_fighter_that_already_uses_it()
    {
        // FR-10: the refusal has to say which fighter the alias is taken by, whether it is
        // that fighter's alias or its own name.
        Assert.Contains("Heat Synced", Replies.AliasTaken("heat", "Heat Synced"), StringComparison.Ordinal);
        Assert.Contains(
            "Heat Synced",
            Replies.AliasIsFighterName("Heat Synced", "Heat Synced"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_fighter_with_more_aliases_than_a_message_holds_lists_the_rest_as_a_count()
    {
        // 100 characters is the longest alias /alias-admin add accepts, so 40 of them cannot fit in
        // one message. The reply has to fit anyway (FR-09 states the wording, not a limit).
        string[] aliases = [.. Enumerable.Range(0, 40).Select(i => $"{i}".PadRight(100, 'a'))];

        string reply = Replies.AliasList("Heat Synced", aliases);

        Assert.True(reply.Length <= Replies.MaxMessageLength);
        Assert.Contains(" more.", reply, StringComparison.Ordinal);
        Assert.StartsWith("Heat Synced is also known as ", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_choice_never_exceeds_what_Discord_displays()
    {
        var suggestion = new Data.Aliases.AliasSuggestion(
            new string('a', 80),
            "aaa",
            new string('b', 80));

        Assert.Equal(Replies.MaxChoiceLength, Replies.ChoiceLabel(suggestion).Length);
    }
}
