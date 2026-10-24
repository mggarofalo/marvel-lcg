using Godot;
using Marvel.Protocol;

namespace Marvel.Godot;

/// <summary>Injected discovery contracts test controls without admitting or starting a product.</summary>
public sealed partial class ScenarioSetupControlsProbe : Node
{
    private static readonly string[] Names = ["Wrecker", "Thunderball", "Piledriver", "Bulldozer"];

    public string Verify(Main main)
    {
        try
        {
            Require(IsInsideTree(), "probe must run in the active scene tree");
            var ordinary = main.setupChoices!;
            var fixtures = Enumerable.Range(0, 16).Select(bits => Choice("custom", bits)).ToList();
            fixtures.Insert(0, Choice("expert", 15)); fixtures.Insert(0, Choice("standard", 0));
            fixtures.Add(Choice("extreme", 0));
            main.setupChoices = ordinary with { Scenarios = [.. fixtures, .. ordinary.Scenarios] };
            main.setupController.PopulateSetupChoices();
            foreach (var fixture in fixtures) VerifyRecipe(main, fixture);
            var heroKeys = ordinary.Heroes.Take(4).Select(hero => hero.Key).ToArray();
            for (int seat = 0; seat < 4; seat++)
            {
                main.setupController.AssignHero(heroKeys[seat], seat);
                main.setupController.RefreshBriefing();
                Require(main.SelectedSetup().HeroKeys.SequenceEqual(heroKeys.Take(seat + 1)), "seat order or distinct hero capture changed");
            }
            main.setupController.AssignHero(heroKeys[1], 0);
            main.setupController.RefreshBriefing();
            Require(main.SelectedSetup().HeroKeys.Count == 1, "duplicate hero was retained in a later seat");
            Select(main.scenario, main.scenarioNames.IndexOf(ordinary.Scenarios[0].Name));
            Require(main.SelectedCampaign().AllowsModularSets, "ordinary modular choices were not restored");
            Require(main.setupGrid.GetNode<Control>("ModularChoices").Visible, "ordinary modular controls stayed hidden");
            Require(main.setupController.VariantControls.Count == 0, "custom component choices survived a scenario change");
            return "";
        }
        catch (Exception error) { return error.ToString(); }
    }

    private static void VerifyRecipe(Main main, ScenarioSetupChoice fixture)
    {
        Select(main.mode, Enumerable.Range(0, main.mode.ItemCount)
            .Single(index => main.mode.GetItemText(index) == ScenarioModeControls.Label(fixture.Mode)));
        if (fixture.Mode == "custom")
        {
            for (int group = 0; group < 4; group++)
            {
                var control = main.setupController.VariantControls[group];
                Select(control, Enumerable.Range(0, control.ItemCount)
                    .Single(index => control.GetItemText(index) == fixture.Variants[group].Value));
            }
        }
        Require(main.SelectedSetup().ScenarioKey == fixture.Key, "controls selected a different authored recipe");
        Require(main.SelectedSetup().ModularKeys.Count == 0, "independent encounter recipe retained modular keys");
        Require(!main.setupGrid.GetNode<Control>("ModularChoices").Visible, "modular selection is visible for independent encounter groups");
    }

    private static ScenarioSetupChoice Choice(string mode, int bits) =>
        new("fixture-" + mode + "-" + bits, "Wrecking discovery fixture", mode == "expert", [])
        {
            Mode = mode, AllowsModularSets = false,
            Variants = Names.Select((name, index) => new ScenarioVariantChoice(name,
                mode == "extreme" ? "A → B" : ((bits >> index) & 1) == 0 ? "A" : "B")).ToArray(),
        };

    private static void Select(OptionButton control, int index)
    {
        control.Select(index);
        control.EmitSignal(OptionButton.SignalName.ItemSelected, index);
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
