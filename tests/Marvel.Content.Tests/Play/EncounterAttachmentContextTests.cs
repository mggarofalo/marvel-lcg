using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EncounterAttachmentContextTests
{
    [Fact]
    [Rule("rr:attach-to")]
    [Rule("rr:you-your")]
    public void NonRevealEntryUsesTheResolvingPlayerWithoutChangingOwnership()
    {
        // rr:attach-to: "the specified game element as it enters play."
        // rr:you-your: "the word 'you' ... must be resolved" as the identity.
        var f = RedSkullFixture.Create(players: 2);
        var attachment = f.Card("02048", DeckType.EncounterDiscardPile);
        var completion = new EffectCompletion(1, 1);
        EncounterEntry.Schedule(f.World, EncounterEntryRequest.For(attachment, 1,
            new CardEntryPlacement(Verb: "Put_Into_Play")), completion);
        var occurrence = f.World.Agenda.Current!.Value.OccurrenceOf(f.World, f.World.Facts);
        Assert.Contains(Steps.CardEntersPlay, occurrence.Conditions);
        Assert.Equal(1, occurrence.Player);

        f.Finish();

        Assert.Equal(1, completion.Applied);
        Assert.Equal(f.World.Seats[1].IdentityCard.ObjectId, attachment.Area.Host);
        Assert.Equal(-1, attachment.Owner);
    }

    [Fact]
    [Rule("rr:attach-to.3.1")]
    public void ExplicitHostOverridesPrintedYouForAnotherResolvingPlayer()
    {
        // "The 'attach to' phrase on a card is not resolved if another ability
        // causes that card to attach to a specific game element."
        var f = RedSkullFixture.Create(players: 2);
        var attachment = f.Card("02048", DeckType.EncounterDiscardPile);
        var completion = new EffectCompletion(1, 1);
        int host = f.World.Seats[0].IdentityCard.ObjectId;
        EncounterEntry.Schedule(f.World, EncounterEntryRequest.For(attachment, 1,
            new CardEntryPlacement(AttachmentTarget: host, Verb: "Put_Into_Play")), completion);

        f.Finish();

        Assert.Equal(1, completion.Applied);
        Assert.Equal(host, attachment.Area.Host);
        Assert.Equal(-1, attachment.Owner);
    }

    [Fact]
    public void PlayerUpgradeQueryDefaultsToItsOwnerAndHonorsExplicitContext()
    {
        // Synthetic placement contract: an owner-bound player upgrade retains
        // its owner context when no encounter resolving player is supplied.
        var f = RedSkullFixture.Create(players: 2);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01008","attachTo":"you","abilities":[]}]}
            """));
        var upgrade = f.World.CreateCard("01008", f.World.AreaOf(
            DeckType.HandsArea, PlayArea.Of(1), cardOwner: 1));
        Assert.Equal([f.World.Seats[1].IdentityCard.ObjectId],
            runner.AttachmentTargets(f.World, upgrade));
        Assert.Equal(f.World.Seats[1].IdentityCard.ObjectId,
            runner.AttachesTo(f.World, upgrade));
        Assert.Equal([f.World.Seats[0].IdentityCard.ObjectId],
            runner.AttachmentTargets(f.World, upgrade, 0));
        Assert.Equal(1, upgrade.Owner);
    }
}
