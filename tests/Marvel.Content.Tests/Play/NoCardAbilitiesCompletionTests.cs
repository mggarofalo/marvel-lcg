using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class NoCardAbilitiesCompletionTests
{
    [Theory]
    [InlineData("02004b", true)]
    [InlineData("01097a", false)]
    public void MissingImplementationRefusesPrintedCompletionButAllowsSilentScheme(
        string face, bool hasCompletion)
    {
        // Engine contract: the explicit empty implementation may omit silent
        // text, but must refuse a printed When Completed ability.
        var f = RedSkullFixture.Create();
        var scheme = f.Card(face, DeckType.MainSchemesArea);
        var abilities = new NoCardAbilities();
        if (hasCompletion)
        {
            var error = Assert.Throws<RulesNotImplementedException>(() =>
                abilities.ObservesMainSchemeCompletion(f.World, scheme));
            Assert.Contains("When Completed", error.Message);
        }
        else Assert.False(abilities.ObservesMainSchemeCompletion(f.World, scheme));
    }
}
