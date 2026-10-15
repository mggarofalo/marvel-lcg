using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TooltipPolicyTests
{
    [Theory]
    [InlineData("Keep hand", "Keep hand", true)]
    [InlineData("Cancel", "Cancel.", true)]
    [InlineData("Copy invitation", " copy  invitation ", true)]
    [InlineData("?", "Undo the previous decision", false)]
    [InlineData("Web-Shooter", "Exhaust this source to generate a wild resource", false)]
    [InlineData("", "Source commit abc123", false)]
    public void OnlyVisibleCaptionRestatementsAreRemoved(string visible, string tooltip, bool expected)
    {
        Assert.Equal(expected, TooltipPolicy.Restates(visible, tooltip));
    }

    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "A very long card title")]
    public void FullCaptionAddsInformationOnlyWhenItsVisibleLabelIsClipped(bool clipped, string expected)
    {
        Assert.Equal(expected, TooltipPolicy.ClippedCaption("A very long card title", clipped));
    }
}
