using System.Net;
using System.Net.Sockets;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ProgramTests
{
    [Fact]
    public void ServerOptionsHaveSafeLocalDefaults()
    {
        Program.ServerOptions options = Program.ServerOptions.Parse([]);

        Assert.Equal(IPAddress.Loopback, options.Address);
        Assert.Equal(41923, options.Port);
        Assert.Equal(Environment.CurrentDirectory, options.DataRoot);
        Assert.Equal(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MarvelLCG",
                "sessions"),
            options.SaveRoot);
        Assert.IsType<PermissiveVisibilityPolicy>(options.Visibility);
        Assert.Null(options.TelemetryEndpoint);
    }

    [Fact]
    public void ServerOptionsAcceptAnExplicitRestrictedSeatEndpointAndDataRoot()
    {
        Program.ServerOptions options = Program.ServerOptions.Parse(
            [
                "--listen", "::1",
                "--port", "65535",
                "--data-root", "content-root",
                "--save-root", "save-root",
                "--visibility", "restricted",
                "--seat", "7",
                "--telemetry-endpoint", "https://telemetry.example.test/v1",
            ]);

        Assert.Equal(IPAddress.IPv6Loopback, options.Address);
        Assert.Equal(65535, options.Port);
        Assert.Equal("content-root", options.DataRoot);
        Assert.Equal("save-root", options.SaveRoot);
        Assert.IsType<RestrictedVisibilityPolicy>(options.Visibility);
        Assert.Equal(
            new Uri("https://telemetry.example.test/v1"),
            options.TelemetryEndpoint);
    }

    [Theory]
    [InlineData("--unknown")]
    [InlineData("--listen")]
    [InlineData("--listen", "localhost")]
    [InlineData("--listen", "0")]
    [InlineData("--listen", "0x0")]
    [InlineData("--listen", "127.1")]
    [InlineData("--listen", "0127.0.0.1")]
    [InlineData("--port")]
    [InlineData("--port", "0")]
    [InlineData("--port", "65536")]
    [InlineData("--port", "+1")]
    [InlineData("--port", "1.0")]
    [InlineData("--data-root")]
    [InlineData("--save-root")]
    [InlineData("--visibility")]
    [InlineData("--visibility", "private")]
    [InlineData("--visibility", "restricted")]
    [InlineData("--seat")]
    [InlineData("--seat", "-1")]
    [InlineData("--seat", "+1")]
    [InlineData("--seat", "one")]
    [InlineData("--seat", "0")]
    [InlineData("--telemetry-endpoint")]
    [InlineData("--telemetry-endpoint", "http://telemetry.example.test/v1")]
    [InlineData("--telemetry-endpoint", "file:///tmp/telemetry")]
    [InlineData("--telemetry-endpoint", "https://user:secret@telemetry.example.test/v1")]
    [InlineData("--telemetry-endpoint", "https://telemetry.example.test/v1?token=secret")]
    public void InvalidServerOptionsAreRejected(params string[] args)
    {
        Assert.Throws<ArgumentException>(() => Program.ServerOptions.Parse(args));
    }

    [Fact]
    public void InvalidContentFailsBeforeTheConfiguredPortStartsListening()
    {
        using var reservation = new TcpListener(IPAddress.Loopback, port: 0);
        reservation.Start();
        int port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        string missingRoot = Path.Combine(
            Path.GetTempPath(), $"marvel-server-missing-{Guid.NewGuid():N}");
        using var error = new StringWriter();

        int result = Program.Run(
            [
                "--listen", IPAddress.Loopback.ToString(),
                "--port", port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "--data-root", missingRoot,
            ],
            error,
            CancellationToken.None);

        Assert.Equal(2, result);
        Assert.DoesNotContain("listening on", error.ToString(), StringComparison.Ordinal);
        Assert.Contains(
            OperationalEventIds.ServerStartFailed,
            error.ToString(),
            StringComparison.Ordinal);
        Assert.DoesNotContain(missingRoot, error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void BindFailureNeverPrintsAFalseReadinessMessage()
    {
        using var reservation = new TcpListener(IPAddress.Loopback, port: 0);
        reservation.Start();
        int port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        using var error = new StringWriter();

        int result = Program.Run(
            [
                "--listen", IPAddress.Loopback.ToString(),
                "--port", port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "--data-root", Marvel.Tests.RepositoryPaths.Root,
            ],
            error,
            CancellationToken.None);

        Assert.Equal(2, result);
        Assert.DoesNotContain("listening on", error.ToString(), StringComparison.Ordinal);
        Assert.Contains(
            OperationalEventIds.ServerStartFailed,
            error.ToString(),
            StringComparison.Ordinal);
        Assert.Single(error.ToString().Split(Environment.NewLine,
            StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void PreCancelledServerNeverBindsOrAnnouncesReadiness()
    {
        using var reservation = new TcpListener(IPAddress.Loopback, port: 0);
        reservation.Start();
        int port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        bool announced = false;
        var server = new SocketEngineServer(
            new EngineHost(DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root)),
            IPAddress.Loopback,
            port);

        server.Run(_ => announced = true, cancelled.Token);

        Assert.False(announced);
    }

    [Fact]
    public void ThrowingReadinessCallbackStillReleasesTheListener()
    {
        IPEndPoint? endpoint = null;
        var server = new SocketEngineServer(
            new EngineHost(DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root)),
            IPAddress.Loopback,
            port: 0);

        Assert.Throws<InvalidOperationException>(() => server.Run(bound =>
        {
            endpoint = bound;
            throw new InvalidOperationException("readiness failed");
        }, TestContext.Current.CancellationToken));

        using var probe = new TcpListener(endpoint!.Address, endpoint.Port);
        probe.Start();
    }

    [Fact]
    public async Task ConfiguredServerAnswersSetupAndCancellationStopsItCleanly()
    {
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        stopping.CancelAfter(TimeSpan.FromSeconds(15));
        using var error = new StringWriter();
        var listening = new TaskCompletionSource<IPEndPoint>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int> running = Task.Run(
            () => Program.Run(
                new Program.ServerOptions(
                    IPAddress.Loopback,
                    Port: 0,
                    Marvel.Tests.RepositoryPaths.Root,
                    new PermissiveVisibilityPolicy(),
                    Path.Combine(Path.GetTempPath(), $"marvel-server-{Guid.NewGuid():N}")),
                error,
                listening.SetResult,
                stopping.Token),
            TestContext.Current.CancellationToken);

        try
        {
            IPEndPoint endpoint = await listening.Task.WaitAsync(
                TimeSpan.FromSeconds(10),
                TestContext.Current.CancellationToken);
            var transport = new SocketTransport(endpoint.Address.ToString(), endpoint.Port);
            EngineResponse response = await transport.ExchangeAsync(
                EngineRequest.ReadSetup("server-ready"),
                TestContext.Current.CancellationToken);

            Assert.Null(response.Error);
            Assert.NotNull(response.Setup);
            Assert.Equal(0, Program.HealthCheck(
                endpoint.Address.ToString(), endpoint.Port));
            stopping.Cancel();
            Assert.Equal(
                0,
                await running.WaitAsync(
                    TimeSpan.FromSeconds(5),
                    TestContext.Current.CancellationToken));
            Assert.Contains(
                OperationalEventIds.ServerListening,
                error.ToString(),
                StringComparison.Ordinal);
        }
        finally
        {
            stopping.Cancel();
            await running.WaitAsync(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public void HealthCheckFailsClosedWhenNoCompatibleServerIsListening()
    {
        using var reservation = new TcpListener(IPAddress.Loopback, port: 0);
        reservation.Start();
        int port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();

        Assert.Equal(1, Program.HealthCheck(IPAddress.Loopback.ToString(), port));
    }
}
