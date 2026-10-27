using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RestrictedResourceAdmissionTests
{
    [Theory]
    [InlineData("Action", "\"aspectCard\"")]
    [InlineData("Action", "\"event\"")]
    [InlineData("Action", "\"attackEvent\"")]
    [InlineData("Action", "\"preparationCard\"")]
    [InlineData("Resource", "\"unknown\"")]
    [InlineData("Resource", "null")]
    [InlineData("Resource", "1")]
    public void InvalidRestrictionCannotEnterCompiledBook(string timing, string restriction)
    {
        Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse($$$$"""
            {"cards":[{"card":"04033","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"{{{{timing}}}}","subject":"game"},
              "resourceRestriction":{{{{restriction}}}},"effect":{"generate":"G"}}]}]}
            """)));
    }
    [Fact]
    public void ProgrammaticAbilityCannotBypassRestrictionAdmission()
    {
        var book = AbilityCatalog.Parse("""
            {"cards":[{"card":"04033","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
              "effect":{"generate":"G"}}]}]}
            """);
        var invalid = book.Abilities[0] with { ResourceRestriction = (Marvel.Rules.Prompts.ResourceRestriction)99 };
        Assert.Throws<AbilityException>(() => AbilityLowering.Book(book with { Abilities = [invalid] }));
        invalid = book.Abilities[0] with
        {
            ResourceRestriction = Marvel.Rules.Prompts.ResourceRestriction.AspectCard,
            Trigger = book.Abilities[0].Trigger with { Timing = Marvel.Rules.Timing.AbilityType.Action },
        };
        Assert.Throws<AbilityException>(() => AbilityLowering.Book(book with { Abilities = [invalid] }));
    }

}
