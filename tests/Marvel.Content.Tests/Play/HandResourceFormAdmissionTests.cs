using Marvel.Cards.Dsl;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HandResourceFormAdmissionTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("false")]
    [InlineData("1")]
    [InlineData("{}")]
    [InlineData("\"Hero\"")]
    [InlineData("\"alter-ego\"")]
    [InlineData("\"\"")]
    public void JsonRejectsUnsupportedHandResourceForms(string value) =>
        Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            "{\"cards\":[{\"card\":\"10007\",\"abilities\":[],\"handResourceForm\":" + value + "}]}"));

    [Theory]
    [InlineData(false, "hero")]
    [InlineData(true, "Hero")]
    public void ProgrammaticLoweringRequiresAnAuthoredFaceAndSupportedForm(bool authored, string form)
    {
        var book = new AbilityBook([], authored ? new HashSet<string> { "10007" } : new HashSet<string>())
        { HandResourceForms = new Dictionary<string, string> { ["10007"] = form } };
        Assert.Throws<AbilityException>(() => AbilityLowering.Book(book));
    }

    [Fact]
    public void FormMetadataCannotSubstituteForCompleteAuthorship() =>
        Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(
            """{"cards":[{"card":"10007","handResourceForm":"hero"}]}"""));
}
