using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

public sealed class AbsorbingActivationTests
{
    [Theory]
    [InlineData("04080", 4, 1)]
    [InlineData("04080", 5, 2)]
    [InlineData("04081", 4, 1)]
    [InlineData("04081", 5, 2)]
    [InlineData("04082", 4, 1)]
    [InlineData("04082", 5, 2)]
    [InlineData("04083", 4, 1)]
    [InlineData("04083", 5, 2)]
    public void UndefendedAttackAppliesTheCurrentEnvironmentAtTheDelayThreshold(string environment, int delay, int amount)
    {
        var f = AbsorbingManFixture.Create(hero: true);
        f.Card(environment, DeckType.EnvironmentArea);
        f.Scheme.PlaceTokens("c_delay", delay);
        f.Villain.TakeDamage(4);
        Hand(f.World, "01003", 3);
        // Synthetic zero-icon boost isolates the printed environment consequence.
        f.Card("04076", DeckType.EncounterDeck);
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: f.Villain.ObjectId, Seat: 0));
        f.Finish(prompt =>
        {
            if (prompt.Cancellable) return Decision.Decline;
            var option = Assert.Single(prompt.Affordances);
            Assert.Equal("discardResourceIcons", option.Label);
            return new Decision(option.Id, f.World.Seats[0].Hand.Cards.Take(amount).Select(card => card.ObjectId).ToArray());
        });
        Assert.Equal(environment == "04080" ? 2 + amount : 2, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(environment == "04081" ? amount : 0, f.Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(environment == "04082" ? 4 - amount : 4, f.Villain.Damage);
        Assert.Equal(environment == "04083" ? 3 - amount : 3, f.World.Seats[0].Hand.Cards.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StageThreeAppliesBothTraitGroupsAfterEitherKindOfActivation(bool attack)
    {
        var f = AbsorbingManFixture.Create(hero: attack);
        World.MoveToTop(f.Villain, f.World.AreaOf(DeckType.RemovedArea));
        var villain = f.Card("04078", DeckType.VillainArea);
        f.Card("04092", DeckType.SideSchemesArea);
        f.Card("04076", DeckType.EncounterDeck);
        f.World.Agenda.Add(new PhaseStep(attack ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            1, 2, Subject: villain.ObjectId, Seat: 0));
        f.Finish();
        Assert.Equal(attack ? 4 : 1, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(attack ? 1 : 3, f.Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(false, true, 2)]
    [InlineData(true, false, 2)]
    [InlineData(true, true, 3)]
    [Rule("rr:lasting-effects.5")]
    public void SwingingStoneModifierBelongsOnlyToItsActivation(bool hero, bool stone, int expected)
    {
        // "A lasting effect expires as soon as its specified duration ends."
        var f = AbsorbingManFixture.Create(hero: hero);
        if (stone) f.Card("04082", DeckType.EnvironmentArea);
        f.Card("04076", DeckType.EncounterDeck);
        var incoming = f.Card("04086");
        f.World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: incoming.ObjectId, Seat: 0));
        f.Finish();
        Assert.Equal(hero ? expected : 0, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(hero ? 0 : expected, f.Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(2, StateFields.Modified(f.World, f.Villain, "attack", f.World.Facts, 1));
        Assert.Equal(1, StateFields.Modified(f.World, f.Villain, "scheme", f.World.Facts, 1));
    }
}
