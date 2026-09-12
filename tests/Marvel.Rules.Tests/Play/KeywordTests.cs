using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public abstract class KeywordTestBase
{
    /// <summary>Runs the attack, declining the defender prompt.</summary>
    protected static void Undefended(World world, Printed printed)
    {
        var abilities = new NoCardAbilities();
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, printed, abilities, events);
        for (int answered = 0; asked is not null; answered++)
        {
            Assert.True(answered < 10, $"'{asked.Label}' is still being asked");
            Sequence.Answer(world, printed, abilities, asked, Decision.Decline, events);
            asked = Sequence.Work(world, printed, abilities, events);
        }
    }

    /// <summary>A treachery in the revealing area, ready for its keywords.</summary>
    protected static Card Treachery(World world) => world.CreateCard("treachery", world.AreaOf(DeckType.RevealingArea));
    protected static World Board(Printed printed, int players = 1)
    {
        var world = new World(printed, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            world.Seats[seat].IdentityCard = world.CreateCard("identity,hero", world.Seats[seat].Hero);
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }

    /// <summary>Gives non-exhaustion tests a replacement encounter deck.</summary>
    protected static void KeepEncounterDeckLive(World world) => world.CreateCard("replacement", world.AreaOf(DeckType.EncounterDeck));
    protected sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string[]> traits = new(StringComparer.Ordinal);
        public Printed With(string faceId, params (string Key, string Value)[] values)
        {
            var table = attributes.TryGetValue(faceId, out var found) ? found : attributes[faceId] = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var(key, value)in values)
            {
                table[key] = value;
            }

            return this;
        }

        public CardKind Kind(string faceId) => faceId switch
        {
            "identity" => CardKind.AlterEgo,
            "hero" => CardKind.Hero,
            "temp" => CardKind.Support,
            "ally" => CardKind.Ally,
            "boost" => CardKind.Treachery,
            "permanentish" => CardKind.Support,
            "attachment" or "permanentAttachment" or "victoryAttachment" => CardKind.Attachment,
            "villain" => CardKind.EncounterVillain,
            "scheme" => CardKind.MainScheme,
            "minion" or "steady" => CardKind.Minion,
            "sideScheme" => CardKind.EncounterSideScheme,
            "obligation" => CardKind.Obligation,
            "tough" => CardKind.Status,
            _ => CardKind.Treachery,
        };
        /// <summary>Traits, upper-cased as the digest spells them.</summary>
        public Printed Trait(string faceId, params string[] names)
        {
            traits[faceId] = names;
            return this;
        }

        public IReadOnlyList<string> Traits(string faceId) => traits.TryGetValue(faceId, out string[]? found) ? found : [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => attributes.TryGetValue(faceId, out var found) ? found : new Dictionary<string, string>(StringComparer.Ordinal);
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => Attributes(faceId).TryGetValue(attribute, out string? value) && long.TryParse(value, out long number) ? number : fallback;
        /// <summary>Stated directly rather than as stars inside ATK/THW.</summary>
        public long ConsequentialDamage(string faceId, string attribute) => attribute == "ATK" ? PrintedValue(faceId, "AtkIcons", 1) : 0;
    }

    protected sealed class ConstantUsesLoss : NoCardAbilities
    {
        private readonly IReadOnlyList<(int Source, int Affected)> losses;
        public ConstantUsesLoss(int source, int affected) : this((source, affected))
        {
        }

        public ConstantUsesLoss(params (int Source, int Affected)[] losses)
        {
            this.losses = losses;
        }

        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card) => [..losses.Where(loss => loss.Source == card.ObjectId).Select(loss => new ContinuousEffect(EffectSource.ConstantAbility, Characteristics.LossOf("uses"), Card: loss.Source, Affects: loss.Affected, Lasts: Duration.WhileInPlay)), ];
    }

    protected sealed class ConstantCharacteristic(int source, int affected, string kind) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card) => card.ObjectId == source ? [new ContinuousEffect(EffectSource.ConstantAbility, kind, Amount: 1, Card: source, Affects: affected, Lasts: Duration.WhileInPlay), ] : [];
    }

    protected sealed class ConstantWhenHostAbsent(int host, int source, string kind) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card) => card.ObjectId == source && !DeckTypes.IsInPlay(world.Cards[host].Area.Type) ? [new ContinuousEffect(EffectSource.ConstantAbility, kind, Amount: 1, Card: source, Affects: source, Lasts: Duration.WhileInPlay), ] : [];
    }

    protected sealed class DependentConstantUsesLoss(int source, int bridge, int affected) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            if (card.ObjectId == source)
            {
                return[new ContinuousEffect(EffectSource.ConstantAbility, Traits.Granted + "ENABLED", Card: source, Affects: bridge, Lasts: Duration.WhileInPlay), ];
            }

            return card.ObjectId == bridge && Traits.Has(world, card, "ENABLED", world.Facts) ? [new ContinuousEffect(EffectSource.ConstantAbility, Characteristics.LossOf("uses"), Card: bridge, Affects: affected, Lasts: Duration.WhileInPlay), ] : [];
        }
    }

    protected sealed class UsesLossWithDependentPermanent(int source, int first, int second, int attachment) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            if (card.ObjectId == source)
            {
                return[new ContinuousEffect(EffectSource.ConstantAbility, Characteristics.LossOf("uses"), Card: source, Affects: first, Lasts: Duration.WhileInPlay), new ContinuousEffect(EffectSource.ConstantAbility, Characteristics.LossOf("uses"), Card: source, Affects: second, Lasts: Duration.WhileInPlay), ];
            }

            return card.ObjectId == second ? [new ContinuousEffect(EffectSource.ConstantAbility, "permanent", Amount: 1, Card: second, Affects: attachment, Lasts: Duration.WhileInPlay), ] : [];
        }
    }

    protected sealed class UsesLossWithDormantPermanent : NoCardAbilities
    {
        private readonly int source;
        private readonly int first;
        private readonly int second;
        private readonly int grantor;
        private readonly int attachment;
        public UsesLossWithDormantPermanent(int source, int first, int second, int attachment) : this(source, first, second, second, attachment)
        {
        }

        public UsesLossWithDormantPermanent(int source, int first, int second, int grantor, int attachment)
        {
            this.source = source;
            this.first = first;
            this.second = second;
            this.grantor = grantor;
            this.attachment = attachment;
        }

        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            if (card.ObjectId == source)
            {
                return[new ContinuousEffect(EffectSource.ConstantAbility, Characteristics.LossOf("uses"), Card: source, Affects: first, Lasts: Duration.WhileInPlay), new ContinuousEffect(EffectSource.ConstantAbility, Characteristics.LossOf("uses"), Card: source, Affects: second, Lasts: Duration.WhileInPlay), ];
            }

            return card.ObjectId == grantor && !DeckTypes.IsInPlay(world.Cards[first].Area.Type) ? [new ContinuousEffect(EffectSource.ConstantAbility, "permanent", Amount: 1, Card: grantor, Affects: attachment, Lasts: Duration.WhileInPlay), ] : [];
        }
    }

    protected sealed class UsesLossWithSurvivingReplacement(int source, int surviving, int affected) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            bool loses = card.ObjectId == source || card.ObjectId == surviving && !DeckTypes.IsInPlay(world.Cards[source].Area.Type);
            return loses ? [new ContinuousEffect(EffectSource.ConstantAbility, Characteristics.LossOf("uses"), Card: card.ObjectId, Affects: affected, Lasts: Duration.WhileInPlay), ] : [];
        }
    }

    protected sealed class ReplacementUsesLossCascade(int source, int second, int third) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            if (card.ObjectId == source)
            {
                return[Loss(source, second)];
            }

            if (card.ObjectId != second)
            {
                return[];
            }

            var effects = new List<ContinuousEffect>
            {
                Loss(second, third)
            };
            if (!DeckTypes.IsInPlay(world.Cards[source].Area.Type))
            {
                effects.Add(Loss(second, second));
            }

            return effects;
        }

        private static ContinuousEffect Loss(int source, int affected) => new(EffectSource.ConstantAbility, Characteristics.LossOf("uses"), Card: source, Affects: affected, Lasts: Duration.WhileInPlay);
    }

    protected sealed class LatchedUsesDeparture(int source, int uses, int bridge) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            if (card.ObjectId == source)
            {
                return[Loss(source, uses)];
            }

            if (card.ObjectId == uses)
            {
                return[new ContinuousEffect(EffectSource.ConstantAbility, Traits.Granted + "ENABLED", Card: uses, Affects: bridge, Lasts: Duration.WhileInPlay), ];
            }

            return card.ObjectId == bridge && !Traits.Has(world, card, "ENABLED", world.Facts) ? [Loss(bridge, uses)] : [];
        }

        private static ContinuousEffect Loss(int source, int affected) => new(EffectSource.ConstantAbility, Characteristics.LossOf("uses"), Card: source, Affects: affected, Lasts: Duration.WhileInPlay);
    }

    protected sealed class PresenceDependentUsesLoss(int presence, int bridge, int affected) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card) => card.ObjectId == bridge && DeckTypes.IsInPlay(world.Cards[presence].Area.Type) ? [new ContinuousEffect(EffectSource.ConstantAbility, Characteristics.LossOf("uses"), Card: bridge, Affects: affected, Lasts: Duration.WhileInPlay), ] : [];
    }

    protected sealed class UsesLossWithSelfPermanentAttachment(int source, int host, int attachment) : NoCardAbilities
    {
        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            if (card.ObjectId == source)
            {
                return[new ContinuousEffect(EffectSource.ConstantAbility, Characteristics.LossOf("uses"), Card: source, Affects: host, Lasts: Duration.WhileInPlay), ];
            }

            return card.ObjectId == attachment ? [new ContinuousEffect(EffectSource.ConstantAbility, "permanent", Amount: 1, Card: attachment, Affects: attachment, Lasts: Duration.WhileInPlay), ] : [];
        }
    }
}
