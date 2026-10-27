using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Unsupported, nonpublic 90002 prototype with synthetic receipt observers.</summary>
internal sealed class RonanJudgeProbeFixture : CoreCardTestBase
{
    internal World World { get; } = new(Cards, 2);
    internal AbilityRunner Runner { get; }
    internal List<GameEvent> Events { get; } = [];
    internal Card Main { get; }
    internal Card Villain { get; }
    internal Card Observer { get; }
    internal Card Judge { get; }

    internal RonanJudgeProbeFixture(string triggerEvent = "WhenAttackEnds")
    {
        // Printed authority: datasets/cards/cards.json, 90002. This prototype is
        // injected into the full book only here; it does not admit Kree Fanatic.
        // Event/role spellings and the observer counters are engine/test choices.
        var local = AbilityCatalog.Parse($$$$"""
            {"cards":[
              {"card":"90002","abilities":[{
                "trigger":{"event":"{{{{triggerEvent}}}}","alsoHappened":"WhenCardDefeated",
                  "timing":"ForcedResponse","subject":"game","actor":"enemy","player":"trigger.player"},
                "when":{"or":[{"isKind":{"card":"defeated","kind":"ally"}},
                  {"isYourIdentity":"defeated"}]},
                "effect":{"placeThreat":{"scheme":{"query":"mainScheme"},"amount":2}}
              },{
                "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
                "effect":{"putIntoPlay":{"card":"this","where":"printedDestination"}}
              }]},
              {"card":"01008","abilities":[{
                "trigger":{"event":"WhenDamageDealt","timing":"ForcedResponse","actor":"enemy","target":"ally"},
                "effect":{"placeCounters":{"card":"this","counter":"received","count":1}}
              },{
                "trigger":{"event":"WhenCardDefeated","timing":"ForcedInterrupt","subject":"game"},
                "when":{"isKind":{"card":"defeated","kind":"ally"}},
                "effect":{"placeCounters":{"card":"this","counter":"defeatInterrupt","count":1}}
              },{
                "trigger":{"event":"WhenAttackEnds","timing":"Response","subject":"game"},
                "effect":{"placeCounters":{"card":"this","counter":"end","count":1}}
              }]}
            ]}
            """);
        Runner = new(AuthoredCards.Book with
        {
            Authored = new HashSet<string>(AuthoredCards.Book.Authored.Concat(["90002"])),
            Abilities = [.. AuthoredCards.Book.Abilities.Where(row => row.Card is not ("90002" or "01008")),
                .. local.Abilities],
        });
        World.Abilities = Runner;
        for (int player = 0; player < 2; player++)
        {
            var seat = World.CreateSeat($"p{player}");
            seat.IdentityCard = World.CreateCard("01001a,01001b", seat.Hero);
            for (int card = 0; card < 8; card++) World.CreateCard("01087", seat.Deck);
        }
        Villain = World.CreateCard("01094", World.AreaOf(DeckType.VillainArea));
        Main = World.CreateCard("01097b", World.AreaOf(DeckType.MainSchemesArea));
        for (int card = 0; card < 12; card++)
            World.CreateCard("01098", World.AreaOf(DeckType.EncounterDeck));
        Observer = World.CreateCard("01008", World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Judge = World.CreateCard("90002", World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)));
        World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: Judge.ObjectId, Seat: 1));
        Finish();
        Assert.Equal(DeckType.SideSchemesArea, Judge.Area.Type);
        Assert.Equal(6, Judge.Tokens.GetValueOrDefault("k_threat"));
    }

    internal Card Ally(int player, bool lethal)
    {
        var ally = World.CreateCard("01041", World.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));
        Assert.Equal(CardKind.Ally, World.Facts.Kind(ally.FaceId));
        Assert.Equal(3, DamagePlacement.Health(World, World.Facts, ally));
        if (lethal) ally.TakeDamage(1);
        return ally;
    }

    internal Prompt AttackUntilResponse(Card ally)
    {
        World.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: Villain.ObjectId, Seat: 1));
        for (int step = 0; step < 50; step++)
        {
            var prompt = Sequence.Work(World, World.Facts, Runner, Events);
            Assert.NotNull(prompt);
            if (prompt.Asking != Question.Defender)
            {
                if (prompt.Affordances.Any(option => option.AnchorId == Observer.ObjectId)) return prompt;
                Assert.True(prompt.Cancellable, prompt.Description);
                Sequence.Answer(World, World.Facts, Runner, prompt, Decision.Decline, Events);
                continue;
            }
            var defense = prompt.Affordances.SingleOrDefault(option => option.AnchorId == ally.ObjectId);
            Sequence.Answer(World, World.Facts, Runner, prompt,
                defense is null ? Decision.Decline : Decision.Take(defense.Id), Events);
        }
        throw new InvalidOperationException("Attack did not reach its response window");
    }

    internal void Finish()
    {
        for (int step = 0; step < 50; step++)
        {
            var prompt = Sequence.Work(World, World.Facts, Runner, Events);
            if (prompt is null)
            {
                Assert.Empty(World.Agenda.Outstanding);
                Assert.Null(World.Activation);
                Assert.Null(World.Attack);
                Assert.False(World.IsOver);
                return;
            }
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(World, World.Facts, Runner, prompt, Decision.Decline, Events);
        }
        Assert.Fail("Probe did not drain its continuation");
    }
}
