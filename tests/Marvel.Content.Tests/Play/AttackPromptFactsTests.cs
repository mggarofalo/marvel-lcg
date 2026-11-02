using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// A decision taken inside an enemy attack carries the attack as facts, so a client can show who attacks
// whom and for how much without reading the description's sentences.
public sealed class AttackPromptFactsTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    // "Give boost card: If a villain ... is attacking, give it one facedown boost card"; until those cards
    // flip, the defender is declared against the enemy's ATK with its boost unresolved.
    [Rule("rr:attack-enemy-activation.1")]
    [Rule("rr:attack-enemy-activation.2")]
    [Fact]
    public void TheDefenderIsAskedAgainstTheAttackersATKWithBoostUnresolved()
    {
        (World world, Card assault, var runner, var events) = Begin(out Prompt? prompt);
        for (int answered = 0; prompt is not null && prompt.Asking != Question.Defender; answered++)
        {
            Assert.True(answered < 6, "no defender was asked for");
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
            prompt = Sequence.Work(world, Cards, runner, events);
        }
        Prompt defense = Assert.IsType<Prompt>(prompt);
        Assert.Equal(Question.Defender, defense.Asking);
        PromptAttack attack = Assert.IsType<PromptAttack>(defense.Attack);
        Card enemy = world.Cards[attack.Attacker];
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, attack.Target);
        Assert.False(attack.Calculated);
        Assert.True(attack.BoostUnresolved);
        Assert.Equal(StateFields.Modified(world, enemy, "attack", Cards, world.Players), attack.Value);
        Assert.Contains($"ATK {attack.Value} before boost icons and defense", defense.Description);
        Assert.NotEqual(assault.ObjectId, attack.Attacker);
    }

    // "The attack finishes resolving and the following types of abilities trigger": once it has finished
    // there is no attack value left to show, so the facts are gone while the description reports the result.
    [Rule("rr:attack-enemy-activation.6")]
    [Fact]
    public void AFinishedAttackCarriesNoAttackFacts()
    {
        (World world, _, var runner, var events) = Begin(out Prompt? prompt);
        bool sawAttack = false;
        for (int answered = 0; prompt is not null; answered++)
        {
            Assert.True(answered < 12, "the attack did not finish after twelve answers");
            if (world.Attack is null) Assert.Null(prompt.Attack);
            else if (prompt.Attack is not null) sawAttack = true;
            Decision decision = prompt.Asking == Question.Defender
                ? Decision.Take(Assert.Single(prompt.Affordances,
                    offer => offer.AnchorId == world.Seats[0].IdentityCard.ObjectId).Id)
                : Decision.Decline;
            Sequence.Answer(world, Cards, runner, prompt, decision, events);
            prompt = Sequence.Work(world, Cards, runner, events);
        }
        Assert.True(sawAttack);
    }

    private static (World World, Card Assault, Marvel.Cards.Run.AbilityRunner Runner,
        List<Marvel.Rules.Events.GameEvent> Events) Begin(out Prompt? prompt)
    {
        World world = WorldSetup.DealWithoutCardAbilities(Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            [Setup.Hero("spider_man").Name], 12345);
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        Card assault = world.CreateCard(AuthoredCards.Assault,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            Round: 1, Number: 4, Subject: assault.ObjectId, Seat: 0));
        var events = new List<Marvel.Rules.Events.GameEvent>();
        prompt = Sequence.Work(world, Cards, runner, events);
        return (world, assault, runner, events);
    }
}
