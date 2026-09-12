using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Release;

internal sealed record ReleaseAcceptanceResults(
    [property: JsonPropertyName("macos_install_and_first_launch")] string MacosInstallAndFirstLaunch,
    [property: JsonPropertyName("windows_community_install_and_trust")] string WindowsCommunityInstallAndTrust,
    [property: JsonPropertyName("windows_portable_install")] string WindowsPortableInstall,
    [property: JsonPropertyName("server_restart_upgrade_and_reconnect")] string ServerRestartUpgradeAndReconnect);
