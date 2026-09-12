using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public abstract class IndirectDamageTestBase
{
    /// <summary>"Aunt May" — a support, so it is not a character.</summary>
    protected const string NotACharacter = "01006";
    /// <summary>Black Cat, the Core Set ally used by hand-built boards.</summary>
    protected const string Ally = "01020";
    protected static readonly SetupCatalog Setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    protected static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    /// <summary>Puts Bomb Scare in play with a stated amount of threat.</summary>
    protected static Card BombScare(World world, long threat)
    {
        var scare = world.CreateCard(AuthoredCards.BombScare, world.AreaOf(DeckType.SideSchemesArea));
        scare.PlaceTokens("k_threat", threat);
        return scare;
    }

    protected static Card Reveal(World world, string faceId)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, card, 0);
        return card;
    }

    protected static World Deal()
    {
        var world = WorldSetup.DealWithoutCardAbilities(Cards, Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards), ["Spider-Man"], 12345);
        world.Abilities = AuthoredCards.Runner();
        return world;
    }

    protected static AbilityRunner HealingAllyRunner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"01020","abilities":[{
          "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt",
                     "subject":"this"},
          "effect":{"heal":{"card":"this","amount":{"damageOn":"this"}}}
        }]}]}
        """));
    protected static AbilityRunner BackflipRunner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"01003","abilities":[{
          "trigger":{"event":"WhenDamageWouldBeDealt","timing":"Interrupt",
                     "target":"you"},
          "when":{"isYourIdentity":"trigger.target"},
          "effect":{"preventDamage":"trigger.target"}
        }]}]}
        """));
    protected static AbilityRunner ForcedSoakAndBackflipRunner() => new(AbilityCatalog.Parse("""
        {"cards":[
          {"card":"01003","abilities":[{
            "trigger":{"event":"WhenDamageWouldBeDealt","timing":"Interrupt",
                       "target":"you"},
            "when":{"isYourIdentity":"trigger.target"},
            "effect":{"preventDamage":"trigger.target"}
          }]},
          {"card":"01098","abilities":[{
            "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt",
                       "subject":"attachedTo"},
            "effect":{"seq":[
              {"soakDamage":{"onto":"this"}},
              {"discard":"this"}
            ]}
          }]}
        ]}
        """));
    protected static AbilityRunner DeclinedDefeatAndSideSchemeRunner() => new(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01020","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt",
                           "subject":"this"},
                "effect":{"heal":{"card":"this","amount":{"damageOn":"this"}}}
              }]},
              {"card":"01109","abilities":[{
                "trigger":{"event":"WhenCardDefeated","timing":"ForcedResponse",
                           "subject":"game"},
                "effect":{"placeThreat":{"scheme":"this","amount":3}}
              }]}
            ]}
            """));
}
