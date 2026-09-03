using Marvel.Tests;
using Xunit;

namespace Marvel.Release.Tests;

public sealed class CommunityReleasePolicyTests
{
    [Fact]
    public void ReleaseWorkflowHasNoPaidDesktopCredentialInputs()
    {
        string workflow = File.ReadAllText(Path.Combine(
            RepositoryPaths.Root,
            ".github",
            "workflows",
            "release-desktop.yml"));
        string ordinaryWorkflow = File.ReadAllText(Path.Combine(
            RepositoryPaths.Root,
            ".github",
            "workflows",
            "ci.yml"));

        Assert.DoesNotContain("${{ secrets.", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("${{ secrets.", ordinaryWorkflow, StringComparison.Ordinal);
        Assert.DoesNotContain("MACOS_CERTIFICATE", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("APPLE_API_", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("WINDOWS_CERTIFICATE", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("WINDOWS_TIMESTAMP", workflow, StringComparison.Ordinal);
        Assert.Contains("CN=Marvel Champions Community", workflow, StringComparison.Ordinal);
        Assert.Contains("desktop-macos-community", workflow, StringComparison.Ordinal);
        Assert.Contains("*-adhoc.zip", workflow, StringComparison.Ordinal);
        Assert.Contains("desktop-windows-community", workflow, StringComparison.Ordinal);
        Assert.Contains("server-linux-signed", workflow, StringComparison.Ordinal);
        Assert.Contains("sign-windows-desktop.ps1", ordinaryWorkflow, StringComparison.Ordinal);
    }

    [Fact]
    public void MacArtifactReplacesInheritedIdentityWithAdHocSigning()
    {
        string script = File.ReadAllText(Path.Combine(
            RepositoryPaths.Root,
            "tools",
            "build-macos-desktop.sh"));

        Assert.Contains("--sign - --timestamp=none", script, StringComparison.Ordinal);
        Assert.Contains("codesign --verify --deep --strict", script, StringComparison.Ordinal);
        Assert.Contains("-macos-adhoc.zip", script, StringComparison.Ordinal);
        Assert.DoesNotContain("MACOS_SIGNING_IDENTITY", script, StringComparison.Ordinal);
    }

    [Fact]
    public void WindowsCommunitySigningCannotExportOrRetainThePrivateKey()
    {
        string script = File.ReadAllText(Path.Combine(
            RepositoryPaths.Root,
            "tools",
            "sign-windows-desktop.ps1"));

        Assert.Contains("-KeyExportPolicy NonExportable", script, StringComparison.Ordinal);
        Assert.Contains("Add-PublicCertificateToStore", script, StringComparison.Ordinal);
        Assert.Contains("TrustedPeople", script, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Add-PublicCertificateToStore $publicStoreCertificate Root",
            script,
            StringComparison.Ordinal);
        Assert.Contains("-DeleteKey", script, StringComparison.Ordinal);
        Assert.Contains("ephemeral signing key cleanup failed", script, StringComparison.Ordinal);
        Assert.Contains("expected self-signed trust verdict", script, StringComparison.Ordinal);
        Assert.Contains("& $signTool sign /fd SHA256 /sha1", script, StringComparison.Ordinal);
        Assert.Contains("input was proven signature-free", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Export-PfxCertificate", script, StringComparison.Ordinal);
        Assert.DoesNotContain("/tr", script, StringComparison.Ordinal);
        Assert.DoesNotContain("TimestampUrl", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleasePageWarnsBeforePublishingCommunityArtifacts()
    {
        string workflow = File.ReadAllText(Path.Combine(
            RepositoryPaths.Root,
            ".github",
            "workflows",
            "release-desktop.yml"));

        Assert.Contains("the macOS app is not notarized", workflow, StringComparison.Ordinal);
        Assert.Contains("self-signed certificate", workflow, StringComparison.Ordinal);
        Assert.Contains("portable ZIP is unsigned", workflow, StringComparison.Ordinal);
        Assert.Contains("Verify the attached SHA-256 files", workflow, StringComparison.Ordinal);
    }
}
