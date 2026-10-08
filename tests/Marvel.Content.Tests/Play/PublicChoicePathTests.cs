using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PublicChoicePathTests
{
    [Fact]
    public void MissingOrAmbiguousAuthoredIdentityCannotAuthorizeAQuantity()
    {
        // Synthetic checked nodes test identity, not structural equality.
        var choice = Choice();
        Assert.True(AbilityPublicChoicePath.AllowsAmount([choice], choice));
        Assert.False(AbilityPublicChoicePath.AllowsAmount([], choice));
        Assert.False(AbilityPublicChoicePath.AllowsAmount([Choice()], choice));
        Assert.False(AbilityPublicChoicePath.AllowsAmount([choice, choice], choice));
    }

    [Fact]
    public void AnUnsupportedAncestorFailsClosedEvenWhenTheLeafIsPublic()
    {
        var choice = Choice();
        var root = new AbilityEffect.ForEach(new AbilityNumber.Constant(1), choice);
        Assert.False(AbilityPublicChoicePath.AllowsAmount([root], choice));
        Assert.True(AbilityPublicChoicePath.AllowsAmount([new AbilityEffect.Sequence([choice])], choice));
    }

    private static AbilityEffect.ChooseCard Choice() => new(
        new AbilityCardSelection.Query(AbilityCardQuery.Villain),
        new AbilityEffect.Damage(new AbilityCardSelection.Bound(AbilityCardBinding.Chosen),
            new AbilityNumber.Constant(20), false));
}
