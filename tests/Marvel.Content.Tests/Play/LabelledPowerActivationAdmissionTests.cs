using Marvel.Rules.Play;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class LabelledPowerActivationAdmissionTests
{
    [Theory]
    [InlineData("{\"query\":\"enemies\"}", "\"chosen\"")]
    [InlineData("\"chosen\"", "{\"query\":\"villain\"}")]
    public void UnsupportedActivationOrDamageSelectionRaisesBeforePayment(string enemies, string recipient)
    {
        // Engine boundary choice: no projected arbitrary activation results.
        var body = LabelledPowerActivationFixture.Body
            .Replace("\"enemies\":\"chosen\"", "\"enemies\":" + enemies, StringComparison.Ordinal)
            .Replace("\"cards\":\"chosen\"", "\"cards\":" + recipient, StringComparison.Ordinal);
        var f = new LabelledPowerActivationFixture(body: body);
        var before = f.World.Digest().Canonical();
        var random = f.World.Random.Generator.GetState();
        long words = f.World.Random.Generator.WordsConsumed;
        var error = Assert.Throws<RulesNotImplementedException>(() => f.Runner.Actions(f.World, 1));
        Assert.Contains("suspends inside a labelled power", error.Message);
        Assert.Equal(before, f.World.Digest().Canonical());
        Assert.Equal(random.Words, f.World.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, f.World.Random.Generator.GetState().Index);
        Assert.Equal(words, f.World.Random.Generator.WordsConsumed);
        Assert.False(f.World.Agenda.IsBusy);
        Assert.Contains(f.Source, f.World.Seats[1].Hand.Cards);
        Assert.Contains(f.Payment, f.World.Seats[1].Hand.Cards);
        // The same fixture and cost have a valid, supported control.
        var control = new LabelledPowerActivationFixture();
        Assert.Single(control.Runner.Actions(control.World, 1), option => option.Card == control.Source.ObjectId);
    }
}
