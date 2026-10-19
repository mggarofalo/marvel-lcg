using Xunit;

namespace Marvel.Godot.Tests;

public sealed class InvitationClipboardTests
{
    [Fact]
    public void FailedClipboardWriteKeepsInvitationForRetry()
    {
        string? invitation = "one-time invitation";
        Assert.False(InvitationClipboard.TryCopy(ref invitation, _ => { }, () => "previous clipboard"));
        Assert.Equal("one-time invitation", invitation);
        string clipboard = "previous clipboard";
        Assert.True(InvitationClipboard.TryCopy(ref invitation, text => clipboard = text, () => clipboard));
        Assert.Equal("one-time invitation", clipboard);
        Assert.Null(invitation);
    }

    [Fact]
    public void MissingInvitationNeverOverwritesClipboard()
    {
        string? invitation = null;
        string clipboard = "existing text";
        Assert.False(InvitationClipboard.TryCopy(ref invitation, text => clipboard = text, () => clipboard));
        Assert.Equal("existing text", clipboard);
    }
}
