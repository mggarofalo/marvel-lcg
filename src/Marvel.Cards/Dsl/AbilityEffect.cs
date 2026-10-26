using System.Collections.Immutable;
using Marvel.Rules.State;
using Marvel.Rules.Play;

namespace Marvel.Cards.Dsl;

/// <summary>A checked, inert instruction in an ability program.</summary>
public abstract record AbilityEffect
{
    private AbilityEffect() { }

    /// <summary>Return this host's stored cards to their owners, optionally removing the host.</summary>
    public sealed record ReleaseBeneath(bool RemoveHost, bool Rescue = false) : AbilityEffect;

    /// <summary>Attach selected encounter attachments to a specified host.</summary>
    public sealed record AttachCards(AbilityCardSelection Cards, AbilityCardSelection Host) : AbilityEffect;

    /// <summary>Store selected cards out of play beneath a host, optionally choosing a random subset.</summary>
    public sealed record StoreBeneath(AbilityCardSelection Cards, AbilityCardSelection Host, bool FaceUp, int? RandomCount) : AbilityEffect;

    /// <summary>Move and shuffle the printed setup selection into a scenario auxiliary deck.</summary>
    public sealed record InitializeAuxiliaryDeck(AuxiliaryDeckKind Deck, AbilityCardSelection Cards) : AbilityEffect;
    /// <summary>Reveal the top card of the named scenario auxiliary deck.</summary>
    public sealed record RevealAuxiliaryTop(AuxiliaryDeckKind Deck) : AbilityEffect;

    /// <summary>Resolve child effects in authored order.</summary>
    public sealed record Sequence(ImmutableArray<AbilityEffect> Effects) : AbilityEffect;
    /// <summary>Resolve independent simultaneous effects in the permitted player order.</summary>
    public sealed record Simultaneous(ImmutableArray<AbilityEffect> Effects) : AbilityEffect;
    /// <summary>Resolve the branch selected by a live condition.</summary>
    public sealed record Conditional(AbilityCondition Test, AbilityEffect? Then, AbilityEffect? Else) : AbilityEffect;
    /// <summary>Resolve the continuation only after full success, or after no application.</summary>
    public sealed record Dependent(AbilityEffect Effect, AbilityEffect Continuation, bool OnFull) : AbilityEffect;
    /// <summary>Resolve an effect separately for each active player.</summary>
    public sealed record EachPlayer(AbilityEffect Effect) : AbilityEffect
    {
        /// <summary>Resolve only for the credited defeating player when specified.</summary>
        public bool DefeaterOnly { get; init; }
        /// <summary>Resolve in printed player order rather than a first-player-chosen order.</summary>
        public bool InPlayerOrder { get; init; }
    }
    /// <summary>Repeat an effect according to a numeric expression.</summary>
    public sealed record ForEach(AbilityNumber Count, AbilityEffect Effect) : AbilityEffect;
    /// <summary>Resolve a follow-up for each matching result of the preceding effect.</summary>
    public sealed record EachTime(AbilityEffect Effect, AbilityCondition When, AbilityEffect Then) : AbilityEffect;
    /// <summary>A player chooses among authored effect options.</summary>
    public sealed record Choose(ImmutableArray<AbilityEffect> Options, ImmutableArray<string> Descriptions) : AbilityEffect;
    /// <summary>A player selects a card before resolving the effect with that binding.</summary>
    public sealed record ChooseCard(AbilityCardSelection From, AbilityEffect Effect) : AbilityEffect
    {
        /// <summary>The first player answers while the original player's resolution bindings remain intact.</summary>
        public bool FirstPlayerChooses { get; init; }
    }
    /// <summary>Resolve an effect after the current enemy activation.</summary>
    public sealed record AfterActivation(AbilityEffect Effect) : AbilityEffect;
    /// <summary>An instruction whose only parameter is a card relation.</summary>
    public sealed record CardAction(AbilityCardInstruction Instruction, AbilityCardSelection Selection) : AbilityEffect;
    /// <summary>Heal damage from the selected card.</summary>
    public sealed record Heal(AbilityCardSelection Card, AbilityNumber Amount) : AbilityEffect;

    /// <summary>Assign a positive remaining value to the selected hit-point dial.</summary>
    public sealed record SetHitPoints(AbilityCardSelection Card, long Remaining) : AbilityEffect;
    /// <summary>Deal ordinary damage, optionally using the attack event verb.</summary>
    public sealed record Damage(AbilityCardSelection Cards, AbilityNumber Amount, bool AttackVerb) : AbilityEffect;
    /// <summary>Deal attack damage with its attack-specific rule handling.</summary>
    public sealed record AttackDamage(AbilityCardSelection Cards, AbilityNumber Amount, bool Overkill) : AbilityEffect
    {
        /// <summary>Whether every recipient receives damage before any recipient is defeated.</summary>
        public bool SimultaneousPlacement { get; init; }
    }
    /// <summary>Move damage between cards, optionally as attack damage.</summary>
    public sealed record MoveDamage(AbilityCardSelection From, AbilityCardSelection To, AbilityNumber Amount, bool Attack) : AbilityEffect;
    /// <summary>Assign indirect damage among eligible characters.</summary>
    public sealed record IndirectDamage(AbilityCardSelection Among, AbilityNumber Amount) : AbilityEffect;
    /// <summary>Give a supported status to every selected card.</summary>
    public sealed record GiveStatus(AbilityCardSelection Cards, string Status) : AbilityEffect;

    /// <summary>Remove every selected status kind from selected in-play characters.</summary>
    public sealed record RemoveStatuses(AbilityCardSelection Cards, ImmutableArray<string> Statuses) : AbilityEffect;
    /// <summary>Change the selected player's form.</summary>
    public sealed record ChangeForm(AbilityPlayer Player, string Form) : AbilityEffect;
    /// <summary>Permit matching cards beneath the source to be played as if in hand.</summary>
    public sealed record AllowPlayBeneath(CardKind Kind, string Trait) : AbilityEffect;
    /// <summary>Draw a fixed number of cards for the selected players.</summary>
    public sealed record Draw(AbilityPlayerSelection Players, int Count) : AbilityEffect;
    /// <summary>Draw to the player's modified or printed hand size.</summary>
    public sealed record DrawToHandSize(AbilityPlayer Player, bool Printed) : AbilityEffect;
    /// <summary>Place threat on selected schemes.</summary>
    public sealed record PlaceThreat(AbilityCardSelection Schemes, AbilityNumber Amount) : AbilityEffect;
    /// <summary>Remove threat, with only the explicitly authored exceptions.</summary>
    public sealed record RemoveThreat(AbilityCardSelection Schemes, AbilityNumber Amount, bool IgnoresCrisis,
        AbilityCardSelection? OverridesCannotFrom) : AbilityEffect
    {
        /// <summary>Remove the quantity assigned to each power target.</summary>
        public bool Assigned { get; init; }
    }
    /// <summary>Move threat between exact selected schemes.</summary>
    public sealed record MoveThreat(AbilityCardSelection From, AbilityCardSelection To, AbilityNumber Amount) : AbilityEffect
    {
        /// <summary>Moves all threat present on the selected source when scheduled.</summary>
        public bool All { get; init; }
    }
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
    /// <summary>A constant discount for the first ally played globally each round.</summary>
    public sealed record ReduceFirstAllyCost(long Amount) : AbilityEffect;
    /// <summary>Shuffle an engine-supported search area.</summary>
    public sealed record Shuffle(AbilitySearchArea Area) : AbilityEffect;
    /// <summary>Pay resources or resolve the alternative effect.</summary>
    public sealed record PayOrEffect(string Resources, AbilityEffect Otherwise, bool ExhaustOnly) : AbilityEffect
    {
        /// <summary>Additional resources with no required type.</summary>
        public int Generic { get; init; }
        /// <summary>Total resources required, including unrestricted resources.</summary>
        public int Amount => checked(Resources.Length + Generic);
    }
    /// <summary>Grant a live trait, continuously or until a timing point.</summary>
    public sealed record GrantTrait(AbilityCardSelection Cards, string Trait, bool EachCard, string? Until) : AbilityEffect;
    /// <summary>Grant a supported modifier, continuously or until a timing point.</summary>
    public sealed record GrantField(AbilityCardSelection Cards, string Field, AbilityNumber Amount, bool EachCard, string? Until) : AbilityEffect
    {
        /// <summary>The printed attack class receiving this keyword, if restricted.</summary>
        public Marvel.Rules.Timing.AttackKeywordRestriction? AttackRestriction { get; init; }
    }
    /// <summary>Define the source's own basic-power base before modifiers.</summary>
    public sealed record DefineBaseValue(string Field, AbilityNumber Value) : AbilityEffect;
    /// <summary>Grant lasting combat modifiers to the live set of characters with a trait.</summary>
    public sealed record GrantTraitCharacters(string Trait, ImmutableArray<string> Fields, long Amount, string Until) : AbilityEffect;
    /// <summary>Grant lasting modifiers to characters controlled by one player.</summary>
    public sealed record GrantControlledCharacters(AbilityPlayer Player, ImmutableArray<string> Fields, AbilityNumber Amount, string Until) : AbilityEffect;
    /// <summary>Prohibit damage to the source from matching damage sources.</summary>
    public sealed record PreventDamageFrom(CardKind SourceKind, string SourceTrait) : AbilityEffect;
    /// <summary>Prohibit damage to the source while a condition holds.</summary>
    public sealed record PreventDamageWhile(AbilityCondition Condition) : AbilityEffect
    {
        /// <summary>An explicitly named protected card; null protects the source itself.</summary>
        public AbilityCardSelection? Target { get; init; }
    }
    /// <summary>Discard the selected card when the named occurrence happens.</summary>
    public sealed record DelayedDiscard(AbilityCardSelection Card, string Condition) : AbilityEffect;
    /// <summary>Stun the character damaged by the bounded attack or activation.</summary>
    public sealed record DelayedStun(string? Within) : AbilityEffect;
    /// <summary>Deal facedown encounter cards to selected players.</summary>
    public sealed record DealEncounterCards(AbilityPlayerSelection Players, int Count) : AbilityEffect;
    /// <summary>Assign top physical player-deck cards the declared minion identity.</summary>
    public sealed record EngageTopAsMinion(AbilityPlayerSelection Players, int Count, string Profile) : AbilityEffect;
    /// <summary>A declarative temporary identity supplied by a constant card definition.</summary>
    public sealed record DefineProfile(EffectiveCardProfile Profile) : AbilityEffect;
    /// <summary>Deal one selected encounter card to a named player.</summary>
    public sealed record DealEncounterCard(AbilityCardSelection Card, AbilityPlayer Player) : AbilityEffect;
    /// <summary>Discard random cards from the selected players' hands.</summary>
    public sealed record DiscardAtRandom(AbilityPlayerSelection Players, AbilityNumber Count) : AbilityEffect;
    /// <summary>Place random cards from selected hands facedown on a host.</summary>
    public sealed record PlaceAtRandom(AbilityPlayerSelection Players, AbilityNumber Count, AbilityCardSelection Host) : AbilityEffect;
    /// <summary>Discard cards from a supported deck or the named players' decks.</summary>
    public sealed record DiscardTop(AbilitySearchArea From, AbilityPlayerSelection? Players, AbilityNumber Count) : AbilityEffect;
    /// <summary>Discard from a bounded deck until a printed match is found.</summary>
    public sealed record DiscardUntil(CardKind? Kind, string? Trait, AbilityDiscardUntilResult Result) : AbilityEffect
    {
        /// <summary>The player explicitly instructed to reveal the match, or the resolving player.</summary>
        public AbilityPlayer Player { get; init; } = AbilityPlayer.You;
        /// <summary>The deck whose current contents bound the discard.</summary>
        public AbilitySearchArea From { get; init; } = AbilitySearchArea.EncounterDeck;
        /// <summary>The printed identity set required for a player-deck match.</summary>
        public string? Set { get; init; }
    }
    /// <summary>Move selected cards into a deck and shuffle it.</summary>
    public sealed record ShuffleInto(AbilityCardSelection Cards, AbilitySearchArea Deck) : AbilityEffect;
    /// <summary>Search ordered areas for a printed face and reveal it.</summary>
    public sealed record Search(string Face, ImmutableArray<AbilitySearchArea> Areas) : AbilityEffect;
    /// <summary>Put a selected card into play by its printed destination or engaged with the resolver.</summary>
    public sealed record PutIntoPlay(AbilityCardSelection Card, bool PrintedDestination) : AbilityEffect
    {
        /// <summary>Enter an ally under the resolving player's control.</summary>
        public bool AllyUnderYourControl { get; init; }
        /// <summary>Engage the first player rather than the resolver.</summary>
        public bool FirstPlayer { get; init; }
    }
    /// <summary>Choose one of the top cards for hand.</summary>
    public sealed record ChooseTopForHand(int Count) : AbilityEffect;
    /// <summary>Privately inspect encounter deck tops and retain the number of treacheries.</summary>
    public sealed record InspectEncounterTop(int Count, string Result) : AbilityEffect;
    /// <summary>Choose differently titled discarded cards to shuffle into the deck.</summary>
    public sealed record ChooseDiscardToShuffle(int Maximum) : AbilityEffect
    {
        /// <summary>Minimum number of selected cards.</summary>
        public int Minimum { get; init; } = 1;
        /// <summary>Whether selected cards must have different printed titles.</summary>
        public bool DifferentTitles { get; init; } = true;
    }
    /// <summary>Discard hand cards providing one or two printed resource icons, as far as possible.</summary>
    public sealed record DiscardResourceIcons(int Count) : AbilityEffect;
    /// <summary>Choose a counted set of cards from your hand to discard.</summary>
    public sealed record DiscardHandCards(AbilityNumber Count) : AbilityEffect;
    /// <summary>Place named counters on a card.</summary>
    public sealed record PlaceCounters(AbilityCardSelection Card, string Counter, AbilityNumber Count) : AbilityEffect;
    /// <summary>Remove named counters from a card.</summary>
    public sealed record RemoveCounters(AbilityCardSelection Card, string Counter, long Count) : AbilityEffect;
    /// <summary>Discard hand cards that generate a specified resource.</summary>
    public sealed record DiscardHandWithResource(char Resource) : AbilityEffect;
    /// <summary>Recover cards discarded this way with a printed resource.</summary>
    public sealed record RecoverDiscardedByResource(char Resource) : AbilityEffect;
    /// <summary>Reduce the next card cost paid by a player.</summary>
    public sealed record ReduceNextCardCost(AbilityPlayer Player, AbilityNumber Amount) : AbilityEffect
    {
        /// <summary>Any player may use the next matching card reduction.</summary>
        public bool AnyPlayer { get; init; }
        /// <summary>Optional printed card kind and trait restriction.</summary>
        public Marvel.Rules.Play.CardCostFilter? Filter { get; init; }
    }
    /// <summary>A labeled attack, defense, or thwart containing an effect.</summary>
    public sealed record Power(AbilityPowerKind Kind, AbilityCardSelection? Target, AbilityEffect Effect, bool AutomaticTarget) : AbilityEffect
    {
        /// <summary>Choose this many distinct attack targets, or all available if fewer exist.</summary>
        public AbilityNumber? TargetCount { get; init; }
    }
    /// <summary>A rule-specific way to select schemes and run a thwart power.</summary>
    public sealed record ThwartGroup(AbilityThwartSelection Selection, AbilityCardSelection Schemes, Power Thwart) : AbilityEffect
    {
        /// <summary>The total threat allocated by a distributed thwart.</summary>
        public int DistributionTotal { get; init; }
    }
    /// <summary>Schedule attacks or schemes by the selected enemies.</summary>
    public sealed record ActivateEnemies(bool Attack, AbilityCardSelection Enemies, AbilityCardSelection? Against,
        bool EngagedHero, bool First, bool Dynamic) : AbilityEffect
    {
        /// <summary>Modify ATK or SCH only for the activations scheduled by this instruction.</summary>
        public AbilityNumber? Modifier { get; init; }
        /// <summary>Whether this attack may receive new boost cards.</summary>
        public bool DealBoostCards { get; init; } = true;
    }
}
