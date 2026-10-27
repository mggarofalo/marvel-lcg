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

/// <summary>Nonpublic actual 90002 board with synthetic receipt observers and optional trigger-composition probes.</summary>
internal sealed class RonanJudgeProbeFixture : CoreCardTestBase
{
    internal World World { get; } = new(Cards, 2);
    internal AbilityRunner Runner { get; }
    internal List<GameEvent> Events { get; } = [];
    internal Card Main { get; }
    internal Card Villain { get; }
    internal Card Observer { get; }
    internal Card Judge { get; }

    internal RonanJudgeProbeFixture(string? triggerEvent = null, string? extraCards = null, int observerPlayer = 0)
    {
        // Default uses the actual 90002 row. Explicit event variants exercise the
        // existing trigger composition without admitting Kree Fanatic.
        // Event/role spellings and the observer counters are engine/test choices.
        var local = AbilityCatalog.Parse($$$$"""
            {"cards":[
              {"card":"90002","abilities":[{
                "trigger":{"event":"{{{{triggerEvent ?? "WhenDamageDealt"}}}}","alsoHappened":"WhenCardDefeated",
                  "timing":"ForcedResponse","subject":"game","actor":"enemy","player":"trigger.player"},
                "when":{"and":[{"damageFromAttack":"trigger"},{"or":[{"isKind":{"card":"defeated","kind":"ally"}},
                  {"isYourIdentity":"defeated"}]}]},
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
        var extra = AbilityCatalog.Parse(extraCards ?? "{\"cards\":[]}");
        var replaced = extra.Abilities.Select(row => row.Card).ToHashSet();
        Runner = new(AuthoredCards.Book with
        {
            Authored = new HashSet<string>(AuthoredCards.Book.Authored.Concat(["90002"])),
            Abilities = [.. AuthoredCards.Book.Abilities.Where(row => row.Card is not ("90002" or "01008") && !replaced.Contains(row.Card)),
                .. (triggerEvent is null
                    ? AuthoredCards.Book.Abilities.Where(row => row.Card == "90002")
                    : local.Abilities.Where(row => row.Card == "90002")),
                .. local.Abilities.Where(row => row.Card == "01008"), .. extra.Abilities],
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
        Observer = World.CreateCard("01008", World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(observerPlayer), cardOwner: observerPlayer));
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

    internal Prompt AttackUntilResponse(Card? ally, int[]? indirect = null, int? responseSource = null)
    {
        World.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: Villain.ObjectId, Seat: 1));
        for (int step = 0; step < 50; step++)
        {
            var prompt = Sequence.Work(World, World.Facts, Runner, Events);
            Assert.True(prompt is not null, $"No response: threat={Main.Tokens.GetValueOrDefault("k_threat")}, defeats={World.Agenda.Occurrence?.Defeats.Count}, finished={World.FinishedAttack?.Defender}, eliminated={World.Seats[1].Eliminated}");
            if (AnswerRequiredChoice(prompt, indirect)) continue;
            if (prompt.Asking != Question.Defender)
            {
                if (prompt.Affordances.Any(option => option.AnchorId == Observer.ObjectId || option.AnchorId == responseSource)) return prompt;
                Assert.True(prompt.Cancellable, prompt.Description);
                Sequence.Answer(World, World.Facts, Runner, prompt, Decision.Decline, Events);
                continue;
            }
            if (indirect is not null) Attack.MakeIndirect(World);
            var defense = prompt.Affordances.SingleOrDefault(option => option.AnchorId == ally?.ObjectId);
            Sequence.Answer(World, World.Facts, Runner, prompt,
                defense is null ? Decision.Decline : Decision.Take(defense.Id), Events);
        }
        throw new InvalidOperationException("Attack did not reach its response window");
    }

    private bool AnswerRequiredChoice(Prompt prompt, int[]? indirect)
    {
        Decision decision;
        if (prompt.Asking == Question.Order) decision = Decision.Take(prompt.Affordances[0].Id);
        else if (prompt.Asking == Question.Element && indirect is not null)
            decision = Decision.Take(prompt.Affordances[0].Id, indirect, []);
        else return false;
        Sequence.Answer(World, World.Facts, Runner, prompt, decision, Events);
        return true;
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
            if (AnswerRequiredChoice(prompt, null)) continue;
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(World, World.Facts, Runner, prompt, Decision.Decline, Events);
        }
        Assert.Fail("Probe did not drain its continuation");
    }
}
