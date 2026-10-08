using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ChoiceAmountAncestryTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void AHiddenAncestorCannotSelectAnUncappedDamageOrThwartQuantity(bool thwart, bool persisted)
    {
        // Synthetic root branches expose the same target and capped outcome.
        // Their private card-dependent constant must not become new public copy.
        string then = Choice(thwart, 20);
        string otherwise = Choice(thwart, 21);
        string effect = $$$$$$"""
            {"if": {
                "test": {"exists": {"withTrait": {
                    "cards": {"cardsIn": {"area":"yourDeck","kind":"Upgrade"}},
                    "trait": "BLACK_PANTHER"
                }}},
                "then": {{{{{{then}}}}}},
                "else": {{{{{{otherwise}}}}}}
            }}
            """;
        var descriptions = new List<string>();
        foreach (string hiddenFace in new[] { "01047", "01088" })
        {
            var runner = Runner(AuthoredCards.AuntMay, "Action", effect);
            Card? source = null;
            var (game, world) = Playing(board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 1);
            }, hero: true, abilities: runner);
            world.CreateCard(hiddenFace, world.Seats[0].Deck);
            Assert.Equal(hiddenFace == "01047", world.Seats[0].Deck.Cards.Any(card => card.FaceId == "01047"));
            Prompt prompt;
            if (persisted)
            {
                var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
                game.Resolve(Decision.Take(action.Id));
                prompt = Assert.IsType<Prompt>(game.Pending);
                PhaseStep current = Assert.IsType<PhaseStep>(world.Agenda.Current);
                Assert.Contains(current.AbilityPath!, frame => frame.StartsWith("if:", StringComparison.Ordinal));
            }
            else
            {
                prompt = Assert.IsType<Prompt>(runner.Choosing(world, source!, 0, 0, null, false));
            }
            string description = Assert.IsType<string>(Assert.Single(prompt.Affordances).Description);
            Assert.DoesNotContain(thwart ? "Current thwart amount" : "Current damage", description);
            descriptions.Add(description);
        }
        Assert.Equal(descriptions[0], descriptions[1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AProvenPublicFinalStepAncestorRetainsTheQuantity(bool thwart)
    {
        string then = Choice(thwart, 4);
        string otherwise = Choice(thwart, 2);
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"if": {"test":{"finalStep":"true"},"then":{{{{{{then}}}}}},"else":{{{{{{otherwise}}}}}}}}
            """);
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 1);
        Prompt prompt = Assert.IsType<Prompt>(runner.Choosing(world, source!, 0, 0, null, true));
        string description = Assert.IsType<string>(Assert.Single(prompt.Affordances).Description);
        Assert.Contains(thwart ? "Current thwart amount: 4 threat" : "Current damage: 4", description);
    }

    private static string Choice(bool thwart, int amount) => thwart
        ? $$$$$$"""
            {"chooseCard": {"from":{"query":"schemes"},"effect":{"thwart":{
                "target":"chosen","effect":{"removeThreat":{"scheme":"chosen","amount":{{{{{{amount}}}}}}}}
            }}}}
            """
        : $$$$$$"""
            {"chooseCard": {"from":{"query":"villain"},"effect":{"seq":[
                {"dealDamage":{"cards":"chosen","amount":{{{{{{amount}}}}}}}},
                {"draw":{"player":"you","count":1}}
            ]}}}
            """;
}
