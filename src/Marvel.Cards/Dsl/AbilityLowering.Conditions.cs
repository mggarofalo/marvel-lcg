using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

public static partial class AbilityLowering
{
    /// <summary>Lowers every operand and field of a supported condition.</summary>
    public static AbilityCondition Condition(AbilityValue value, AbilityLocation location)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(location);
        var node = Operation(value, location);
        var child = location.Child(node.Kind);
        var argument = node.Argument;
        return node.Kind switch
        {
            "and" => new AbilityCondition.All(Conditions(argument, child)),
            "or" => new AbilityCondition.Any(Conditions(argument, child)),
            "not" => new AbilityCondition.Negated(Condition(argument, child)),
            "finalStep" => Flag(argument, child, "true", AbilityConditionFact.FinalStep),
            "canMakeTheCall" => Flag(argument, child, "game", AbilityConditionFact.CanMakeTheCall),
            "attackDamaged" => Flag(argument, child, "trigger.target", AbilityConditionFact.AttackDamaged),
            "inExpertMode" => Flag(argument, child, "expert", AbilityConditionFact.InExpertMode),
            "defeatedByYou" => Flag(argument, child, "you", AbilityConditionFact.DefeatedByYou),
            "heroDefended" => Flag(argument, child, "you", AbilityConditionFact.HeroDefended),
            "undefendedAttack" => Flag(argument, child, "you", AbilityConditionFact.UndefendedAttack),
            "defeatedBy" => Flag(argument, child, "consequentialDamage", AbilityConditionFact.DefeatedByConsequentialDamage),
            "paidWithResource" => new AbilityCondition.PaidWithResource(Resource(argument, child)),
            "discardedWithResource" => new AbilityCondition.DiscardedWithResource(Resource(argument, child)),
            "threatCause" => new AbilityCondition.CausedThreat(Text(argument, child) switch
            {
                "villainPhase" => ThreatCause.VillainPhase,
                "enemyScheme" => ThreatCause.EnemyScheme,
                "incite" => ThreatCause.Incite,
                "cardAbility" => ThreatCause.CardAbility,
                var cause => throw child.Error($"'{cause}' is not a threat cause"),
            }),
            "exists" => new AbilityCondition.Exists(Cards(argument, child)),
            "canLegalPractice" => new AbilityCondition.LegalPractice(Cards(argument, child)),
            "canAutomaticThwart" => new AbilityCondition.AutomaticThwart(Cards(argument, child)),
            "titleInPlay" => new AbilityCondition.TitleInPlay(Text(argument, child)),
            "atLeast" => AtLeast(argument, child),
            "inForm" => InForm(argument, child),
            "activationIs" => new AbilityCondition.ActivationIs(Text(argument, child) switch
            {
                "attack" => true,
                "scheme" => false,
                var kind => throw child.Error($"'{kind}' is not an enemy activation kind"),
            }),
            "hasStatus" => CardText(argument, child, "status", AbilityCardTextProperty.Status),
            "hasTrait" => CardText(argument, child, "trait", AbilityCardTextProperty.Trait),
            "cardSet" => CardText(argument, child, "set", AbilityCardTextProperty.Set),
            "isTitle" => CardText(argument, child, "title", AbilityCardTextProperty.Title),
            "isKind" => IsKind(argument, child),
            "wasDefeated" => new AbilityCondition.WasDefeated(Cards(argument, child)),
            "isYourIdentity" => new AbilityCondition.IsYourIdentity(Cards(argument, child)),
            _ => throw child.Error($"'{node.Kind}' is not a condition"),
        };
    }

    /// <summary>Lowers a player relation without consulting the board.</summary>
    public static AbilityPlayer Player(AbilityValue value, AbilityLocation location)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(location);
        return Text(value, location) switch
        {
            "trigger.player" => AbilityPlayer.TriggerPlayer,
            "you" => AbilityPlayer.You,
            "controller" => AbilityPlayer.Controller,
            "chosenPlayer" => AbilityPlayer.ChosenPlayer,
            "engagedPlayer" => AbilityPlayer.EngagedPlayer,
            "firstPlayer" => AbilityPlayer.FirstPlayer,
            var name => throw location.Error($"'{name}' is not a player relation"),
        };
    }

    private static ImmutableArray<AbilityCondition> Conditions(AbilityValue value, AbilityLocation location)
    {
        if (value is not AbilityValue.List list)
        {
            throw location.Error("expected a list of conditions");
        }
        var builder = ImmutableArray.CreateBuilder<AbilityCondition>(list.Values.Count);
        for (int index = 0; index < list.Values.Count; index++)
        {
            builder.Add(Condition(list.Values[index], location.Item(index)));
        }
        return builder.MoveToImmutable();
    }

    private static AbilityCondition.Flag Flag(
        AbilityValue value, AbilityLocation location, string expected, AbilityConditionFact kind)
    {
        FixedWord(value, location, expected);
        return new(kind);
    }

    private static AbilityCondition.AtLeast AtLeast(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "value", "count");
        return new(Number(Required(fields, "value", location), location.Child("value")),
            Number(Required(fields, "count", location), location.Child("count")));
    }

    private static AbilityCondition.InForm InForm(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "player", "form");
        string form = Text(Required(fields, "form", location), location.Child("form"));
        if (form is not ("hero" or "alter-ego"))
        {
            throw location.Child("form").Error($"'{form}' is not a supported form");
        }
        return new(Player(Required(fields, "player", location), location.Child("player")), form);
    }

    private static AbilityCondition.CardText CardText(
        AbilityValue value, AbilityLocation location, string name, AbilityCardTextProperty property)
    {
        var fields = Fields(value, location, "card", name);
        string text = Text(Required(fields, name, location), location.Child(name));
        if (property == AbilityCardTextProperty.Status
            && text is not (Statuses.Tough or Statuses.Stunned or Statuses.Confused))
        {
            throw location.Child(name).Error($"'{text}' is not a status");
        }
        return new(Cards(Required(fields, "card", location), location.Child("card")), property, text);
    }

    private static AbilityCondition.IsKind IsKind(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "kind");
        var kind = ConditionCardKind(Required(fields, "kind", location), location.Child("kind"));
        return new(Cards(Required(fields, "card", location), location.Child("card")), kind);
    }

    private static CardKind ConditionCardKind(AbilityValue value, AbilityLocation location)
    {
        string name = Text(value, location);
        return name switch
        {
            "sideScheme" => CardKind.EncounterSideScheme,
            "minion" => CardKind.Minion,
            "ally" => CardKind.Ally,
            "upgrade" => CardKind.Upgrade,
            "treachery" => CardKind.Treachery,
            _ => throw location.Error($"'{name}' is not a supported condition card kind"),
        };
    }
}
