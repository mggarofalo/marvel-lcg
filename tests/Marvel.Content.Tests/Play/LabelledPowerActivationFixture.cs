using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic local book; does not admit Hulk content to the product.</summary>
internal sealed class LabelledPowerActivationFixture
{
    internal const string Body = """
        {"chooseCard":{"from":{"query":"attackableEnemies"},"effect":{
          "attack":{"target":"chosen","effect":{"seq":[
            {"enemyAttacks":{"enemies":"chosen"}},
            {"dealAttackDamage":{"cards":"chosen","amount":5}}
          ]}}
        }}}
        """;
    internal static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    internal readonly AbilityRunner Runner;
    internal readonly World World = new(Cards, players: 2);
    internal readonly List<GameEvent> Events = [];
    internal readonly Card Source;
    internal readonly Card Payment;
    internal readonly Card Enemy;
    internal readonly Card Ally;
    internal readonly Card Observer;
    internal Prompt? Pending;

    internal LabelledPowerActivationFixture(bool villain = true, string? body = null, bool includeChase = false, bool includeMoment = false)
    {
        var book = AbilityCatalog.Parse("""
            {"cards":[{"card":"10015","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "effect":BODY
            }]},{"card":"01092","abilities":[{
              "trigger":{"event":"WhenBoostCardTurnedFaceup","timing":"Interrupt","actor":"villain"},
              "effect":{"placeCounters":{"card":"you","counter":"boost_seen","count":1}}
            },{
              "trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"hero"},
              "effect":{"placeCounters":{"card":"you","counter":"attack_seen","count":1}}
            }]}]}
            """.Replace("BODY", body ?? Body, StringComparison.Ordinal));
        if (includeChase) book = book with
        {
            Abilities = [.. book.Abilities, .. AuthoredCards.Book.On("01052")],
            Authored = book.Authored.Append("01052").ToHashSet(StringComparer.Ordinal),
        };
        if (includeMoment)
        {
            var moment = AbilityCatalog.Parse("""
                {"cards":[{"card":"12030","abilities":[{
                  "trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"you","target":"enemy","form":"hero"},
                  "when":{"wasDefeated":"trigger.target"},
                  "effect":{"heal":{"card":"you","amount":{"attackExcessDamage":"trigger.target"}}}
                }]}]}
                """);
            book = book with
            {
                Abilities = [.. book.Abilities, .. moment.Abilities],
                Authored = book.Authored.Append("12030").ToHashSet(StringComparer.Ordinal),
            };
        }
        Runner = new AbilityRunner(book);
        for (int player = 0; player < 2; player++)
        {
            var seat = World.CreateSeat($"p{player}");
            seat.IdentityCard = World.CreateCard("01001a", seat.Hero);
            for (int index = 0; index < 10; index++) World.CreateCard("01089", seat.Deck);
        }
        World.Abilities = Runner;
        var enemy = World.CreateCard("01094", World.AreaOf(DeckType.VillainArea));
        Enemy = villain ? enemy : World.CreateCard("01103",
            World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        World.Effects.Register(new(Marvel.Rules.Timing.EffectSource.LastingEffect, "health", 10,
            Affects: Enemy.ObjectId));
        Ally = World.CreateCard("01002", World.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Observer = World.CreateCard("01092", World.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        Source = World.CreateCard("10015", World.Seats[1].Hand);
        Payment = World.CreateCard("01088", World.Seats[1].Hand);
        for (int index = 0; index < 10; index++) World.CreateCard("01104", World.AreaOf(DeckType.EncounterDeck));
        World.CreateCard("01102", World.AreaOf(DeckType.EncounterDeck));
    }

    internal void Start()
    {
        var action = Assert.Single(Runner.Actions(World, 1), option => option.Card == Source.ObjectId);
        Events.AddRange(Runner.ExecuteAction(World, new PlayerAction(action, [Payment.ObjectId], [])).Events);
        Pending = Sequence.Work(World, Cards, Runner, Events);
        Assert.Equal(Question.Element, Pending!.Asking);
        Answer(Decision.Take(Enemy.ObjectId));
    }

    internal void Answer(Decision decision)
    {
        Sequence.Answer(World, Cards, Runner, Pending!, decision, Events);
        Pending = Sequence.Work(World, Cards, Runner, Events);
    }

    internal void Finish()
    {
        for (int count = 0; Pending is not null && count < 20; count++)
        {
            Assert.True(Pending.Cancellable);
            Answer(Decision.Decline);
        }
        Assert.Null(Pending);
        Assert.False(World.IsOver);
        Assert.False(World.Agenda.IsBusy);
    }
}
