using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>A checked, inert instruction in an ability program.</summary>
public abstract record AbilityEffect
{
    private AbilityEffect() { }

    /// <summary>Resolve child effects in authored order.</summary>
    public sealed record Sequence(ImmutableArray<AbilityEffect> Effects) : AbilityEffect;
    /// <summary>Resolve independent simultaneous effects in the permitted player order.</summary>
    public sealed record Simultaneous(ImmutableArray<AbilityEffect> Effects) : AbilityEffect;
    /// <summary>Resolve the branch selected by a live condition.</summary>
    public sealed record Conditional(AbilityCondition Test, AbilityEffect? Then, AbilityEffect? Else) : AbilityEffect;
    /// <summary>Resolve the continuation only after full success, or after no application.</summary>
    public sealed record Dependent(AbilityEffect Effect, AbilityEffect Continuation, bool OnFull) : AbilityEffect;
    /// <summary>Resolve an effect separately for each active player.</summary>
    public sealed record EachPlayer(AbilityEffect Effect) : AbilityEffect;
    /// <summary>Repeat an effect according to a numeric expression.</summary>
    public sealed record ForEach(AbilityNumber Count, AbilityEffect Effect) : AbilityEffect;
    /// <summary>Resolve a follow-up for each matching result of the preceding effect.</summary>
    public sealed record EachTime(AbilityEffect Effect, AbilityCondition When, AbilityEffect Then) : AbilityEffect;
    /// <summary>A player chooses among authored effect options.</summary>
    public sealed record Choose(ImmutableArray<AbilityEffect> Options, ImmutableArray<string> Descriptions) : AbilityEffect;
    /// <summary>A player selects a card before resolving the effect with that binding.</summary>
    public sealed record ChooseCard(AbilityCardSelection From, AbilityEffect Effect) : AbilityEffect;
    /// <summary>Resolve an effect after the current enemy activation.</summary>
    public sealed record AfterActivation(AbilityEffect Effect) : AbilityEffect;
    /// <summary>An instruction whose only parameter is a card relation.</summary>
    public sealed record CardAction(AbilityCardInstruction Instruction, AbilityCardSelection Selection) : AbilityEffect;
    /// <summary>Heal damage from the selected card.</summary>
    public sealed record Heal(AbilityCardSelection Card, AbilityNumber Amount) : AbilityEffect;
    /// <summary>Deal ordinary damage, optionally using the attack event verb.</summary>
    public sealed record Damage(AbilityCardSelection Cards, AbilityNumber Amount, bool AttackVerb) : AbilityEffect;
    /// <summary>Deal attack damage with its attack-specific rule handling.</summary>
    public sealed record AttackDamage(AbilityCardSelection Cards, AbilityNumber Amount, bool Overkill) : AbilityEffect;
    /// <summary>Move damage between cards, optionally as attack damage.</summary>
    public sealed record MoveDamage(AbilityCardSelection From, AbilityCardSelection To, AbilityNumber Amount, bool Attack) : AbilityEffect;
    /// <summary>Assign indirect damage among eligible characters.</summary>
    public sealed record IndirectDamage(AbilityCardSelection Among, AbilityNumber Amount) : AbilityEffect;
    /// <summary>Give a supported status to every selected card.</summary>
    public sealed record GiveStatus(AbilityCardSelection Cards, string Status) : AbilityEffect;
    /// <summary>Change the selected player's form.</summary>
    public sealed record ChangeForm(AbilityPlayer Player, string Form) : AbilityEffect;
    /// <summary>Draw a fixed number of cards for the selected players.</summary>
    public sealed record Draw(AbilityPlayerSelection Players, int Count) : AbilityEffect;
    /// <summary>Draw to the player's modified or printed hand size.</summary>
    public sealed record DrawToHandSize(AbilityPlayer Player, bool Printed) : AbilityEffect;
    /// <summary>Place threat on selected schemes.</summary>
    public sealed record PlaceThreat(AbilityCardSelection Schemes, AbilityNumber Amount) : AbilityEffect;
    /// <summary>Remove threat, with only the explicitly authored exceptions.</summary>
    public sealed record RemoveThreat(AbilityCardSelection Schemes, AbilityNumber Amount, bool IgnoresCrisis,
        AbilityCardSelection? OverridesCannotFrom) : AbilityEffect;
    /// <summary>Prevent an amount of the imminent threat.</summary>
    public sealed record PreventThreat(AbilityNumber Amount) : AbilityEffect;
    /// <summary>Prevent damage to the interrupted occurrence's recipient.</summary>
    public sealed record PreventDamage(AbilityNumber Amount) : AbilityEffect;
    /// <summary>Gain this many instances of Surge.</summary>
    public sealed record GainSurge(long Instances) : AbilityEffect;
    /// <summary>An operation whose authored argument is a fixed marker.</summary>
    public sealed record Fixed(AbilityFixedInstruction Instruction) : AbilityEffect;
    /// <summary>Generate a fixed sequence of resource symbols.</summary>
    public sealed record Generate(string Resources) : AbilityEffect;
    /// <summary>A constant resource multiplier for a printed classification.</summary>
    public sealed record DoubleResourceFor(string Classification) : AbilityEffect;
    /// <summary>Shuffle an engine-supported search area.</summary>
    public sealed record Shuffle(AbilitySearchArea Area) : AbilityEffect;
    /// <summary>Pay resources or resolve the alternative effect.</summary>
    public sealed record PayOrEffect(string Resources, AbilityEffect Otherwise, bool ExhaustOnly) : AbilityEffect;
    /// <summary>Grant a live trait, continuously or until a timing point.</summary>
    public sealed record GrantTrait(AbilityCardSelection Cards, string Trait, bool EachCard, string? Until) : AbilityEffect;
    /// <summary>Grant a supported modifier, continuously or until a timing point.</summary>
    public sealed record GrantField(AbilityCardSelection Cards, string Field, AbilityNumber Amount, bool EachCard, string? Until) : AbilityEffect;
    /// <summary>Grant modifiers to all characters controlled by the named player.</summary>
    public sealed record GrantControlledCharacters(AbilityPlayer Player, ImmutableArray<string> Fields, AbilityNumber Amount, string Until) : AbilityEffect;
    /// <summary>Prohibit damage to the source from matching damage sources.</summary>
    public sealed record PreventDamageFrom(CardKind SourceKind, string SourceTrait) : AbilityEffect;
    /// <summary>Prohibit damage to the source while a condition holds.</summary>
    public sealed record PreventDamageWhile(AbilityCondition Condition) : AbilityEffect;
    /// <summary>Discard the selected card when the named occurrence happens.</summary>
    public sealed record DelayedDiscard(AbilityCardSelection Card, string Condition) : AbilityEffect;
    /// <summary>Stun the character damaged by the bounded attack or activation.</summary>
    public sealed record DelayedStun(string? Within) : AbilityEffect;
    /// <summary>Deal facedown encounter cards to selected players.</summary>
    public sealed record DealEncounterCards(AbilityPlayerSelection Players, int Count) : AbilityEffect;
    /// <summary>Turn top player-deck cards into engaged Drone minions.</summary>
    public sealed record CreateDrones(AbilityPlayerSelection Players, int Count) : AbilityEffect;
    /// <summary>Deal one selected encounter card to a named player.</summary>
    public sealed record DealEncounterCard(AbilityCardSelection Card, AbilityPlayer Player) : AbilityEffect;
    /// <summary>Discard random cards from the selected players' hands.</summary>
    public sealed record DiscardAtRandom(AbilityPlayerSelection Players, AbilityNumber Count) : AbilityEffect;
    /// <summary>Place random cards from selected hands facedown on a host.</summary>
    public sealed record PlaceAtRandom(AbilityPlayerSelection Players, AbilityNumber Count, AbilityCardSelection Host) : AbilityEffect;
    /// <summary>Discard cards from a supported deck or the named players' decks.</summary>
    public sealed record DiscardTop(AbilitySearchArea From, AbilityPlayerSelection? Players, AbilityNumber Count) : AbilityEffect;
    /// <summary>Discard from the encounter deck until a matching card is found.</summary>
    public sealed record DiscardUntil(CardKind Kind, string? Trait, bool PutIntoPlayForFirstPlayer) : AbilityEffect;
    /// <summary>Move selected cards into a deck and shuffle it.</summary>
    public sealed record ShuffleInto(AbilityCardSelection Cards, AbilitySearchArea Deck) : AbilityEffect;
    /// <summary>Search ordered areas for a printed face and reveal it.</summary>
    public sealed record Search(string Face, ImmutableArray<AbilitySearchArea> Areas) : AbilityEffect;
    /// <summary>Put a selected card into play by its printed destination or engaged with the resolver.</summary>
    public sealed record PutIntoPlay(AbilityCardSelection Card, bool PrintedDestination) : AbilityEffect;
    /// <summary>Choose one of the top cards for hand.</summary>
    public sealed record ChooseTopForHand(int Count) : AbilityEffect;
    /// <summary>Choose differently titled discarded cards to shuffle into the deck.</summary>
    public sealed record ChooseDiscardToShuffle(int Maximum) : AbilityEffect;
    /// <summary>Place named counters on a card.</summary>
    public sealed record PlaceCounters(AbilityCardSelection Card, string Counter, AbilityNumber Count) : AbilityEffect;
    /// <summary>Remove named counters from a card.</summary>
    public sealed record RemoveCounters(AbilityCardSelection Card, string Counter, long Count) : AbilityEffect;
    /// <summary>Discard hand cards that generate a specified resource.</summary>
    public sealed record DiscardHandWithResource(char Resource) : AbilityEffect;
    /// <summary>Recover cards discarded this way with a printed resource.</summary>
    public sealed record RecoverDiscardedByResource(char Resource) : AbilityEffect;
    /// <summary>Reduce the next card cost paid by a player.</summary>
    public sealed record ReduceNextCardCost(AbilityPlayer Player, AbilityNumber Amount) : AbilityEffect;
    /// <summary>A labeled attack, defense, or thwart containing an effect.</summary>
    public sealed record Power(AbilityPowerKind Kind, AbilityCardSelection? Target, AbilityEffect Effect, bool AutomaticTarget) : AbilityEffect;
    /// <summary>A rule-specific way to select schemes and run a thwart power.</summary>
    public sealed record ThwartGroup(AbilityThwartSelection Selection, AbilityCardSelection Schemes, Power Thwart) : AbilityEffect;
    /// <summary>Schedule attacks or schemes by the selected enemies.</summary>
    public sealed record ActivateEnemies(bool Attack, AbilityCardSelection Enemies, AbilityCardSelection? Against,
        bool EngagedHero, bool First, bool Dynamic) : AbilityEffect;
}
