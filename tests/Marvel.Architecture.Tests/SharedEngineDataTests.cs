using System.Collections;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Session;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class SharedEngineDataTests
{
    [Fact]
    public void PublishedVocabulariesCannotBeChangedByAConsumer()
    {
        Assert.Throws<NotSupportedException>(() => ((IList)LabeledAbilities.Known)[0] = "Invented");
        Assert.Equal(new[] { BasicPowers.AttackVerb, Attack.DefenseVerb, BasicPowers.ThwartVerb }, LabeledAbilities.Known);
        Assert.Throws<NotSupportedException>(() => ((IList)Resources.Types)[0] = '?');
        foreach (IReadOnlySet<string> vocabulary in new[]
            { Keywords.Granted, AbilityRoles.All, AbilitySubjects.All, StepConditions.EveryCondition })
        {
            Assert.DoesNotContain("Invented", vocabulary);
            if (vocabulary is ICollection<string> mutable)
                Assert.Throws<NotSupportedException>(() => mutable.Add("Invented"));
        }
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, string>)StateFieldCatalog.FilledFrom)["attack"] = "Invented");
    }

    [Fact]
    public void CodecOptionsAreSealedBeforeTheFirstConsumerCanConfigureThem()
    {
        // Architecture choice: a caller cannot change later games' wire spelling.
        foreach (System.Text.Json.JsonSerializerOptions options in
            new[] { EventJson.Options, JournalJson.Options, SessionSaveJson.Options })
        {
            Assert.True(options.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => options.WriteIndented = !options.WriteIndented);
        }
    }

    [Fact]
    public void AnActionsEmptyVariablesCannotBecomeDefaultsForAnotherAction()
    {
        var first = new PlayerAction(new(1, AbilityType.Response, 0), [], []);
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, long>)first.DefinedValues).Add("X", 9));
        var second = new PlayerAction(new(2, AbilityType.Response, 0), [], []);
        Assert.Empty(first.DefinedValues);
        Assert.Empty(second.DefinedValues);
    }
}
