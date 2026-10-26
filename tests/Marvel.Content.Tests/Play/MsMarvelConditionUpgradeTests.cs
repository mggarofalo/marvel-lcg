using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MsMarvelConditionUpgradeTests
{
    [Theory]
    [InlineData("05023", "health", 3, 0)]
    [InlineData("05023", "health", 3, 1)]
    [InlineData("05033", "recover", 2, 0)]
    [InlineData("05033", "recover", 2, 1)]
    public void PlayTransfersControlAppliesPrintedBonusAndEnforcesMaxOne(string face, string field, int bonus, int recipient)
    {
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        var runner = AuthoredCards.Runner();
        var identity = world.Seats[recipient].IdentityCard;
        if (face == "05033") identity.TurnTo(recipient == 0 ? "01001b" : "01010b");
        long Value() => StateFields.Modified(world, identity, field, world.Facts, world.Players);
        long before = Value();
        var card = world.CreateCard(face, world.Seats[0].Hand);
        var pay = world.CreateCard("01003", world.Seats[0].Hand);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], card, [pay.ObjectId], [], [identity.ObjectId]);
        Assert.Equal(DeckType.UpgradesArea, card.Area.Type);
        Assert.Equal(recipient, card.Area.PlayArea.Player);
        Assert.Equal(before + bonus, Value());
        var duplicate = world.CreateCard(face, world.Seats[0].Hand);
        Assert.DoesNotContain(identity.ObjectId, CardPlay.LegalAttachmentTargets(world, world.Facts, world.Seats[0], duplicate, runner)!);
        Assert.Contains(world.Seats[1 - recipient].IdentityCard.ObjectId,
            CardPlay.LegalAttachmentTargets(world, world.Facts, world.Seats[0], duplicate, runner)!);
        var duplicatePay = world.CreateCard("01003", world.Seats[0].Hand);
        string digest = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, world.Facts, runner,
            world.Seats[0], duplicate, [duplicatePay.ObjectId], [], [identity.ObjectId]));
        Assert.Equal(digest, world.Digest().Canonical());
        if (face == "05023")
        {
            identity.TurnTo(recipient == 0 ? "01001b" : "01010b");
            Assert.Equal(before + bonus, Value());
        }
        Discard.Card(world, card, "test", []);
        Assert.Equal(before, Value());
        Assert.Contains(identity.ObjectId, CardPlay.LegalAttachmentTargets(world, world.Facts, world.Seats[0], duplicate, runner)!);
    }
}
