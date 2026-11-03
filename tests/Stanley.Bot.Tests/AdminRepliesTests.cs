using Stanley.Bot.Presentation;
using Stanley.Data.Fighters;
using Stanley.Data.Wiki;

namespace Stanley.Bot.Tests;

/// <summary>
/// What the editor commands say: a refused write has to state the rule it broke, and a
/// confirmed edit the old and new values.
/// </summary>
public class AdminRepliesTests
{
    [Fact]
    public void A_confirmed_edit_shows_the_old_and_new_value_of_each_field()
    {
        string reply = AdminReplies.FighterEdited(
            FighterWriteResult.Edited(
                "Heat Synced",
                [
                    new FighterFieldChange(FighterField.BaseAtk, "617", "1234"),
                    new FighterFieldChange(FighterField.Quote, null, "Hello."),
                ]),
            "heat synced");

        Assert.Contains("Heat Synced", reply, StringComparison.Ordinal);
        Assert.Contains("base attack: 617 → 1234", reply, StringComparison.Ordinal);

        // A field that held nothing says so, rather than reading as an empty line.
        Assert.Contains("quote: (not set) → Hello.", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_changed_raid_boss_flag_reads_as_yes_or_no()
    {
        // The repository hands the flag over as the stored value; the wording is decided here.
        string reply = AdminReplies.FighterEdited(
            FighterWriteResult.Edited(
                "Doomsayer",
                [new FighterFieldChange(FighterField.RaidBoss, "false", "true")]),
            "doomsayer");

        Assert.Contains("Raid Boss flag: No → Yes", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_long_list_of_changes_ends_in_a_count_of_the_rest()
    {
        // Twelve changed fields, each as long as a value is allowed to be, outgrow a Discord
        // message; the reply has to fit one anyway.
        string value = new('x', 500);

        List<FighterFieldChange> changes =
        [
            .. Enum.GetValues<FighterField>()
                .Select(field => new FighterFieldChange(field, value, value)),
        ];

        string reply = AdminReplies.FighterEdited(
            FighterWriteResult.Edited("Heat Synced", changes),
            "heat synced");

        Assert.True(reply.Length <= Replies.MaxMessageLength);
        Assert.Contains("more.", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_refused_name_names_the_fighter_that_already_answers_to_it()
    {
        // The FRD words this for aliases, and a fighter's own name follows the same rule.
        string reply = AdminReplies.FighterAdded(
            FighterWriteResult.NameTaken("Heat", "Heat Synced"),
            "Heat");

        Assert.Contains("Heat Synced", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_refused_value_states_the_rule_it_broke()
    {
        string stat = AdminReplies.FighterAdded(
            FighterWriteResult.NotPositive(FighterField.BaseHp),
            "Heat Synced");

        Assert.Contains("base health", stat, StringComparison.Ordinal);
        Assert.Contains("greater than zero", stat, StringComparison.Ordinal);

        string url = AdminReplies.FighterEdited(
            FighterWriteResult.NotAUrl(FighterField.PortraitUrl, "portrait.png"),
            "Heat Synced");

        Assert.Contains("portrait URL", url, StringComparison.Ordinal);
        Assert.Contains("https://", url, StringComparison.Ordinal);

        string rarity = AdminReplies.FighterAdded(
            FighterWriteResult.NotAllowed(FighterField.Rarity, "Platinum"),
            "Heat Synced");

        Assert.Contains("Platinum", rarity, StringComparison.Ordinal);
    }

    [Fact]
    public void A_fighter_that_matched_nothing_gets_the_same_reply_as_a_lookup()
    {
        // The FRD words this once, and an editor typing a name that matches nothing is in the
        // same position as anyone else.
        Assert.Equal(
            Replies.NoMatch("nosuchfighter"),
            AdminReplies.FighterEdited(FighterWriteResult.NotFound(), "nosuchfighter"));
    }

    [Fact]
    public void A_refused_sa_number_says_which_numbers_are_allowed()
    {
        string reply = AdminReplies.AbilityNotPrompted(
            AbilityPrompt.OutOfRange("Heat Synced", 2),
            "heat synced");

        // Abilities are added at the end, so SA1 to SA3 for a fighter that has two.
        Assert.Contains("between 1 and 3", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void An_added_ability_and_a_rewritten_one_are_confirmed_differently()
    {
        string added = AdminReplies.AbilityWritten(
            AbilitySetResult.Saved("Heat Synced", 3, wasAdded: true, abilityCount: 3));

        Assert.Contains("Added SA3", added, StringComparison.Ordinal);

        string rewritten = AdminReplies.AbilityWritten(
            AbilitySetResult.Saved("Heat Synced", 1, wasAdded: false, abilityCount: 2));

        Assert.Contains("SA1", rewritten, StringComparison.Ordinal);
        Assert.DoesNotContain("Added", rewritten, StringComparison.Ordinal);
    }

    [Fact]
    public void A_removal_is_confirmed_with_what_goes_with_the_fighter()
    {
        // The aliases go with the fighter, so the question says so before anything does.
        string one = AdminReplies.ConfirmRemoveFighter(
            new FighterTarget(1, "Heat Synced", AbilityCount: 1, AliasCount: 1));

        Assert.Contains("1 signature ability", one, StringComparison.Ordinal);
        Assert.Contains("1 alias", one, StringComparison.Ordinal);

        string many = AdminReplies.ConfirmRemoveFighter(
            new FighterTarget(1, "Doomsayer", AbilityCount: 5, AliasCount: 0));

        Assert.Contains("5 signature abilities", many, StringComparison.Ordinal);
        Assert.Contains("0 aliases", many, StringComparison.Ordinal);
    }

    [Fact]
    public void Removing_abilities_says_they_are_entered_again_from_the_first()
    {
        // The all-or-nothing rule is the surprising part, so both the question and the
        // confirmation say it.
        Assert.Contains(
            "SA1",
            AdminReplies.ConfirmRemoveAbilities(new FighterTarget(1, "Doomsayer", 5, 0)),
            StringComparison.Ordinal);

        Assert.Contains(
            "SA1",
            AdminReplies.AbilitiesRemoved(AbilityRemovalResult.Removed("Doomsayer", 5)),
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_confirmed_wiki_page_names_the_key_and_the_address()
    {
        string reply = AdminReplies.WikiPageAdded(
            WikiPageWriteResult.Added("critless", "https://example.com/wiki/Critless"),
            "critless");

        Assert.Contains("critless", reply, StringComparison.Ordinal);
        Assert.Contains("https://example.com/wiki/Critless", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wiki_page_key_that_finds_a_fighter_is_refused_naming_the_fighter()
    {
        // Pages resolve before fighters, so the refusal has to say what would be hidden.
        string reply = AdminReplies.WikiPageAdded(
            WikiPageWriteResult.KeyFindsFighter("Heat Synced", "Heat Synced"),
            "Heat Synced");

        Assert.Contains("Heat Synced", reply, StringComparison.Ordinal);
        Assert.Contains("hide it", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wiki_page_key_another_page_holds_is_refused_naming_that_page()
    {
        string reply = AdminReplies.WikiPageAdded(
            WikiPageWriteResult.KeyTaken("CRITLESS", "critless"),
            "CRITLESS");

        Assert.Contains("critless", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wiki_page_key_with_no_letters_or_digits_says_why_it_cannot_work()
    {
        string reply = AdminReplies.WikiPageAdded(
            WikiPageWriteResult.KeyNotUsable("???"),
            "???");

        Assert.Contains("letters or digits", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wiki_page_address_that_is_not_a_url_names_what_was_given()
    {
        string reply = AdminReplies.WikiPageAdded(
            WikiPageWriteResult.NotAUrl("fandom.com/wiki/Critless"),
            "critless");

        Assert.Contains("https://", reply, StringComparison.Ordinal);
        Assert.Contains("fandom.com/wiki/Critless", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_blank_wiki_page_address_names_the_option_it_is_about()
    {
        Assert.Contains(
            "address",
            AdminReplies.WikiPageAdded(WikiPageWriteResult.UrlMissing(), "critless"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_confirmed_wiki_page_edit_shows_the_old_and_new_value_of_each_field()
    {
        string reply = AdminReplies.WikiPageEdited(
            WikiPageWriteResult.Edited(
                "critless",
                "https://example.com/b",
                [
                    new WikiPageFieldChange(WikiPageField.Key, "crit", "critless"),
                    new WikiPageFieldChange(
                        WikiPageField.Url,
                        "https://example.com/a",
                        "https://example.com/b"),
                ]),
            "crit");

        Assert.Contains("critless", reply, StringComparison.Ordinal);
        Assert.Contains("key: crit → critless", reply, StringComparison.Ordinal);
        Assert.Contains(
            "address: https://example.com/a → https://example.com/b",
            reply,
            StringComparison.Ordinal);
    }

    [Fact]
    public void An_edit_that_gave_nothing_says_what_to_give()
    {
        string reply = AdminReplies.WikiPageEdited(
            WikiPageWriteResult.NothingGiven(),
            "critless");

        Assert.Contains("key", reply, StringComparison.Ordinal);
        Assert.Contains("address", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_removed_wiki_page_names_what_went()
    {
        string reply = AdminReplies.WikiPageRemoved(
            new RemovedWikiPage("critless", "https://example.com/wiki/Critless"));

        Assert.Contains("critless", reply, StringComparison.Ordinal);
        Assert.Contains("https://example.com/wiki/Critless", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wiki_page_that_does_not_exist_is_not_reported_as_a_missing_fighter()
    {
        // /wiki-admin takes a page, not a fighter, so the fighter wording would mislead.
        string reply = AdminReplies.NoWikiPageMatch("critles");

        Assert.Contains("No wiki page matches", reply, StringComparison.Ordinal);
        Assert.DoesNotContain("fighter", reply, StringComparison.Ordinal);
    }
}
