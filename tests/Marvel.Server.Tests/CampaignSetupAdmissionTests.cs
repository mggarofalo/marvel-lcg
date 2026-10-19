using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class CampaignSetupAdmissionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly CampaignDefinition Definition = RiseOfRedSkullCampaign.Definition;

    [Fact]
    public void DuplicateWeaponsAndDuplicatePhysicalObligationsFailBeforeDealing()
    {
        var setup = Initial();
        Assert.Throws<ArgumentException>(() => Validate(setup with { ExperimentalWeapons = ["04072", "04072"] }));
        var seat = setup.Seats[0];
        var duplicate = seat.AvailableObligations[0] with { Key = "duplicate" };
        Assert.Throws<ArgumentException>(() => Validate(setup with { Seats = [seat with { Cards = [duplicate] }] }));
        Assert.Throws<ArgumentException>(() => Validate(setup with
        { Seats = [seat with { AvailableObligations = seat.AvailableObligations.RemoveAt(0) }] }));
        Validate(setup);
        Assert.Equal(4, setup.Seats[0].AvailableObligations.Length);
    }

    [Theory]
    [InlineData(0, false, 0)]
    [InlineData(3, true, 0)]
    [InlineData(3, false, -1)]
    [InlineData(3, false, 4)]
    public void RecoveryCannotBeOptionalAtZeroOrUseAnUnearnedHealthBonus(int hp, bool recovery, int bonus)
    {
        var setup = Initial();
        setup = setup with { Stage = CampaignStage.AbsorbingMan,
            Seats = [setup.Seats[0] with { RecordedHitPoints = hp, RequiresRecovery = recovery, ConditionHitPointBonus = bonus }] };
        Assert.Throws<ArgumentException>(() => Validate(setup));
    }

    [Fact]
    public void EarnedConditionBonusAndSkippedRosterSeatsAreAdmitted()
    {
        var setup = Initial();
        var condition = Definition.Conditions[0];
        setup = setup with { Stage = CampaignStage.Taskmaster,
            Seats = [setup.Seats[0] with { RecordedHitPoints = 5, Cards = [condition.Basic],
                ConditionHitPointBonus = condition.HitPointBonus, CampaignSeat = 2 }] };
        Validate(setup);
        Assert.Equal(2, setup.Seats[0].CampaignSeat);
        Assert.Throws<ArgumentException>(() => Validate(setup with { Seats = [setup.Seats[0] with { CampaignSeat = -1 }] }));
    }

    private static CampaignGameSetup Initial() => CampaignRules.Setup(CampaignRules.Create(Definition,
        [new("spider_man", new PlayerDeckRecipe(["01002"]), 10)], true, 7));

    private static void Validate(CampaignGameSetup setup) => CampaignSetupAdmission.Validate(
        new GameSpecification(Definition.Scenarios[(int)setup.Stage].Standard, ["spider_man"], [], 7,
            [new(["01002"])], setup), Definition, Cards, Cards.Faces.ToHashSet(StringComparer.Ordinal));
}
