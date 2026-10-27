using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Content.Tests.Play;

internal sealed class HulkNemesisFixture : ChoosingCardsTestBase
{
    internal static (World World, AbilityRunner Runner) Create()
    {
        var world = Deal("spider_man", "captain_marvel");
        world.Agenda.Abandon();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        return (world, runner);
    }

    internal static Card Dealt(World world, string face, int player = 0)
    {
        var card = world.CreateCard(face,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            Round: 1, Number: 4, Subject: card.ObjectId, Seat: player));
        return card;
    }
    internal static void Defend(World world, AbilityRunner runner, int player, Card ally)
    {
        for (int i = 0; ; i++)
        {
            Assert.True(i < 20);
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            if (prompt.Asking == Question.Defender && prompt.Player == player)
            {
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(ally.ObjectId), []);
                return;
            }
            Assert.True(prompt.Cancellable);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }
    }

}
