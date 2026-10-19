using Marvel.Rules.Campaign;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Tests.Campaign;

/// <summary>Synthetic printed cards exercise campaign procedure contracts without admitting expansion content.</summary>
internal static class CampaignRecoveryFixture
{
    internal static readonly CampaignCard ObligationA = new("obligation-a", "obligation-a", CampaignCardRole.Obligation);
    internal static readonly CampaignCard ObligationB = new("obligation-b", "obligation-b", CampaignCardRole.Obligation);

    internal static Game Start(CampaignGameSetup setup, bool healthBonus = false, bool forceOpeningObligation = false, bool handPenalty = false)
    {
        var facts = new Printed();
        var blueprints = new List<CardBlueprint>
        {
            new("alterego,hero", SetupSlot.Identity, 0),
            new("scheme-a,scheme-b", SetupSlot.MainScheme, -1),
            new("villain", SetupSlot.Villain, -1),
        };
        var bindings = new List<CampaignCardBinding>();
        foreach (var seat in setup.Seats)
        {
            if (seat.Seat != 0) blueprints.Add(new("alterego,hero", SetupSlot.Identity, seat.Seat));
            for (int index = 0; index < 8; index++) blueprints.Add(new("player", SetupSlot.PlayerDeck, seat.Seat));
            foreach (var obligation in new[] { ObligationA, ObligationB }.Select(card =>
                seat.Seat == 0 ? card : card with { Key = $"{seat.Seat}-{card.Key}" }))
            {
                bindings.Add(new(blueprints.Count, seat.Seat, obligation));
                blueprints.Add(new(obligation.Face, seat.Cards.Contains(obligation)
                    ? SetupSlot.PlayerDeck : SetupSlot.SetAside, seat.Seat));
            }
        }
        blueprints.Add(new("weapon", SetupSlot.SetAside, -1));
        for (int index = 0; index < 6; index++) blueprints.Add(new("encounter", SetupSlot.Encounter, -1));
        var abilities = new Abilities(healthBonus, forceOpeningObligation, handPenalty);
        World world = WorldSetup.DealConfigured(facts, blueprints, setup.Seats.Select(seat => $"Test hero {seat.Seat}").ToArray(), 17, abilities,
            new WorldSetupOptions(Campaign: setup, CampaignCards: bindings));
        return Game.Begin(world, facts, abilities);
    }

    internal static CampaignGameSetup Setup(bool expert = true, CampaignStage stage = CampaignStage.AbsorbingMan,
        int hp = 5, bool required = false, bool available = true) =>
        new("synthetic", expert, stage, 1,
            [new(0, [], hp, required, available ? [ObligationA, ObligationB] : [], false)], [], 0);

    private sealed class Abilities(bool healthBonus, bool forceOpeningObligation, bool handPenalty) : NoCardAbilities
    {
        public override AbilityExecutionResult Setup(World world, AbilityExecution entry, Card card)
        {
            if (forceOpeningObligation && card.FaceId == "villain")
                World.MoveToTop(world.Cards.Single(value => value.FaceId == "obligation-a"), world.Seats[0].Deck);
            return world.Agenda.Executions.FinishBatch(entry, []);
        }

        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card) =>
            healthBonus && card == world.Seats[0].IdentityCard
                ? [new(EffectSource.ConstantAbility, "health", 3, Card: card.ObjectId, Affects: card.ObjectId)]
                : handPenalty && card.FaceId == "obligation-a"
                    ? [new(EffectSource.ConstantAbility, "hand_size", -1, Card: card.ObjectId,
                        Affects: world.Seats[0].IdentityCard.ObjectId)] : [];
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "alterego" => CardKind.AlterEgo, "hero" => CardKind.Hero,
            "scheme-a" or "scheme-b" => CardKind.MainScheme,
            "villain" => CardKind.EncounterVillain,
            "weapon" => CardKind.Attachment,
            "obligation-a" or "obligation-b" => CardKind.Obligation,
            "encounter" => CardKind.Treachery,
            _ => CardKind.Event,
        };
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>
            { ["name"] = faceId, ["HP"] = "10", ["HS"] = "3" };
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => attribute switch
            { "HP" => 10, "HS" => 3, "StartingThreat" => faceId.StartsWith("scheme", StringComparison.Ordinal) ? 1 : 0, "Threat" => 99, _ => fallback };
    }
}
