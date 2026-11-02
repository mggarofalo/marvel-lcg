using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.GalaxyModularBoards;

namespace Marvel.Content.Tests.Play;

// The production Kree Militants modular set on synthetic boards.
public sealed class KreeMilitantsTests : CoreCardTestBase
{
    private const string HydraMercenary = "01101";

    [Fact]
    [Rule("rr:attach-to")]
    public void KreeCombatArmorAttachesToTheEnemyWithTheHighestAttack()
    {
        // 16131: "Attach to the enemy with the highest ATK." Starshark (ATK 3) outranks Rhino (ATK 2).
        var world = Board();
        var shark = GrootBoards.Engaged(world, Starshark);
        var armor = ShipCommandBoards.Reveal(world, KreeCombatArmor);

        Drive(world);

        Assert.Equal(shark.ObjectId, armor.Area.Host);
    }

    [Fact]
    [Rule("rr:first-player.1")]
    public void TheFirstPlayerChoosesBetweenEnemiesTiedForTheHighestAttack()
    {
        // Kree Commando and Rhino both have ATK 2; player 1 reveals and player 0 chooses.
        var world = Board(players: 2);
        var commando = GrootBoards.Engaged(world, KreeCommando, player: 1);
        var armor = ShipCommandBoards.Reveal(world, KreeCombatArmor, player: 1);
        Prompt? asked = null;

        Drive(world, prompt =>
        {
            if (prompt.Trigger != Steps.ChooseAttachmentTarget) return null;
            asked = prompt;
            return Decision.Take(prompt.Affordances.Single(a => a.AnchorId == commando.ObjectId).Id);
        });

        Assert.Equal(0, asked!.Player);
        Assert.Equal(2, asked.Affordances.Count);
        Assert.Equal(commando.ObjectId, armor.Area.Host);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void KreeCombatArmorReducesTheDamageOfEachAttackByOne()
    {
        // "Reduce the amount of damage attached character takes from each attack by 1."
        var world = Board();
        var villain = GrootBoards.Villain(world);
        ShipCommandBoards.Attach(world, villain, KreeCombatArmor);

        BasicPowers.BasicAttack(world, Cards, 0, villain, []);
        Drive(world);

        Assert.Equal(1, villain.Damage);
    }

    [Fact]
    [Rule("rr:cost")]
    public void KreeCombatArmorIsDiscardedForThreeResourcesOfOneType()
    {
        // "Hero Action: Spend 3 resources of the same type → discard this card." Genius and
        // Enhanced Spider-Sense make three mental resources: only the mental action is offered.
        var world = Board();
        var armor = ShipCommandBoards.Attach(world, GrootBoards.Villain(world), KreeCombatArmor);
        var genius = GrootBoards.Hand(world, ShipCommandBoards.Genius);
        var sense = GrootBoards.Hand(world, "01004");
        GrootBoards.Hand(world, "01003");

        var offered = ShipCommandBoards.ActionsOn(world, armor, 0);
        ShipCommandBoards.Act(world, armor, 0, genius, sense);

        Assert.Single(offered);
        Assert.Equal(DeckType.EncounterDiscardPile, armor.Area.Type);
    }

    [Fact]
    [Rule("rr:wild-resource.1")]
    public void AWildResourceCompletesThreeOfOneTypeAndMixedTypesDoNot()
    {
        // Energy (two energy) and Backflip (physical) are not three of one type; Spider-Woman's
        // wild resource declared as energy is.
        var world = Board();
        var armor = ShipCommandBoards.Attach(world, GrootBoards.Villain(world), KreeCombatArmor);
        GrootBoards.Hand(world, ShipCommandBoards.Energy);
        GrootBoards.Hand(world, "01003");

        Assert.Empty(ShipCommandBoards.ActionsOn(world, armor, 0));

        GrootBoards.Hand(world, "01011");
        Assert.Single(ShipCommandBoards.ActionsOn(world, armor, 0));
    }

    [Fact]
    [Rule("rr:piercing")]
    public void KreeCommandoAsABoostCardGivesTheAttackPiercing()
    {
        // 16132: "Boost: If this is an attack, this attack gains piercing." Its two icons make 4.
        var world = Board();
        var hero = world.Seats[0].IdentityCard;
        var villain = GrootBoards.Villain(world);
        Statuses.Give(world, hero, Statuses.Tough);
        OnTop(world, KreeCommando);

        VillainAttacks(world);

        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
        Assert.Equal(4, hero.Damage);
        Assert.Equal(0, StateFields.Modified(world, villain, "piercing", world.Facts, world.Players));
    }

    [Theory]
    [InlineData(false, 5, 0)]
    [InlineData(true, 0, 1)]
    [Rule("rr:boost-boost-icon")]
    public void KreeLieutenantAddsThreeBoostIconsOnlyToAnAttack(bool scheme, long damage, long threat)
    {
        // 16133: "Boost: If this activation is an attack, this card gets +3 boost icons."
        var world = Board(face: scheme ? "01001b" : "01001a");
        OnTop(world, KreeLieutenant);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme, 0);
        Drive(world);

        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(threat, GrootBoards.MainScheme(world).Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(KreePrivate, 1)]
    [InlineData(HydraMercenary, 0)]
    [Rule("rr:overkill")]
    public void KreePrivateAsABoostCardGivesTheAttackOverkill(string boost, long excess)
    {
        // 16134: "Boost: If this activation is an attack, this attack gains overkill." Rhino
        // attacks for 3 with one boost icon; Black Cat (2 hit points) defends. Hydra Mercenary
        // also has one icon and no ability.
        var world = Board();
        var hero = world.Seats[0].IdentityCard;
        var cat = Ally(world);
        OnTop(world, boost);

        VillainAttacks(world, answer: prompt => prompt.Asking == Question.Defender ? Decision.Take(cat.ObjectId) : null);

        Assert.Equal(DeckType.DiscardPile, cat.Area.Type);
        Assert.Equal(excess, hero.Damage);
    }

    [Fact]
    [Rule("rr:quickstrike")]
    public void KreePrivateAttacksTheHeroItEngages()
    {
        var world = Board();
        ShipCommandBoards.Reveal(world, KreePrivate);

        Drive(world);

        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }
}
