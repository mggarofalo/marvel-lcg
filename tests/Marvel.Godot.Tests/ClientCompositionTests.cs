using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ClientCompositionTests
{
    public static TheoryData<string> InvalidEndpoints => new()
    {
        "localhost:41923",
        "http://localhost:41923",
        "tcp://localhost",
        "tcp://localhost:0",
        "tcp://localhost:65536",
        "tcp://user:secret@localhost:41923",
        "tcp://localhost:41923/game",
        "tcp://localhost:41923?secret=query",
        "tcp://localhost:41923#secret-fragment",
        " tcp://localhost:41923",
        "tcp://localhost:41923\n",
        $"tcp://{new string('a', 254)}:41923",
        new string('x', 513),
    };

    [Fact]
    public async Task MissingConfigurationUsesTheEmbeddedEngine()
    {
        LocalClientConnection connection = ClientComposition.Connect(
            RepositoryPaths.Root, configuredEndpoint: string.Empty);

        Assert.True(connection.Succeeded);
        ClientSetupResult setup = await connection.Client!.ReadSetupAsync(
            TestContext.Current.CancellationToken);
        Assert.True(setup.Succeeded, setup.Error?.Message);
    }

    [Theory]
    [MemberData(nameof(InvalidEndpoints))]
    public void InvalidConfigurationNeverFallsBackOrLeaksItsValue(string endpoint)
    {
        string missingContent = Path.Combine(
            Path.GetTempPath(), "marvel-missing-content", Guid.NewGuid().ToString("N"));

        LocalClientConnection connection = ClientComposition.Connect(
            missingContent, endpoint);

        Assert.False(connection.Succeeded);
        Assert.Null(connection.Client);
        Assert.Equal("invalid_endpoint", connection.Error?.Code);
        Assert.DoesNotContain(endpoint, connection.Error?.Message, StringComparison.Ordinal);
        Assert.True(connection.Error?.Message.Length <= 240);
    }

    [Fact]
    public async Task ExplicitSocketConfigurationDoesNotReadLocalContent()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task serving = ServeOneAsync(listener);
        string missingContent = Path.Combine(
            Path.GetTempPath(), "marvel-missing-content", Guid.NewGuid().ToString("N"));

        try
        {
            LocalClientConnection connection = ClientComposition.Connect(
                missingContent, $"tcp://127.0.0.1:{port}");

            Assert.True(connection.Succeeded);
            ClientSetupResult setup = await connection.Client!.ReadSetupAsync(
                TestContext.Current.CancellationToken);
            Assert.True(setup.Succeeded, setup.Error?.Message);
        }
        finally
        {
            listener.Stop();
            try
            {
                await serving;
            }
            catch (SocketException) when (!listener.Server.IsBound)
            {
            }
        }
    }

    [Fact]
    public async Task UnreachableConfiguredServiceBecomesABoundedProductError()
    {
        int unusedPort;
        using (var listener = new TcpListener(IPAddress.Loopback, 0))
        {
            listener.Start();
            unusedPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        LocalClientConnection connection = ClientComposition.Connect(
            dataRoot: "content-must-not-be-read-for-remote-composition",
            $"tcp://127.0.0.1:{unusedPort}");
        ClientSetupResult setup = await connection.Client!.ReadSetupAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal("transport_unavailable", setup.Error?.Code);
        Assert.True(setup.Error?.Message.Length <= 240);
    }

    [Fact]
    public async Task FutureSocketSchemaIsReportedAsAnUnsupportedVersion()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        byte[] response = Encoding.UTF8.GetBytes(
            "{\"version\":" + (EngineProtocol.Version + 1)
            + ",\"request_id\":\"local-setup\",\"game_id\":\"\",\"capability\":null,"
            + "\"prompt\":null,\"events\":[],\"future_field\":{\"shape\":\"unknown\"}}");
        Task serving = ServeResponseAsync(listener, response);

        try
        {
            LocalGameClient client = ClientComposition.Connect(
                dataRoot: "content-must-not-be-read-for-remote-composition",
                $"tcp://127.0.0.1:{port}").Client!;

            ClientSetupResult setup = await client.ReadSetupAsync(
                TestContext.Current.CancellationToken);

            Assert.Equal("unsupported_version", setup.Error?.Code);
        }
        finally
        {
            listener.Stop();
            try
            {
                await serving;
            }
            catch (SocketException) when (!listener.Server.IsBound)
            {
            }
        }
    }

    private static Task ServeOneAsync(TcpListener listener) => Task.Run(() =>
    {
        using TcpClient accepted = listener.AcceptTcpClient();
        new SocketEngineServer(
            new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root)),
            IPAddress.Loopback,
            port: 0).Serve(accepted);
    });

    private static Task ServeResponseAsync(TcpListener listener, byte[] response) =>
        Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            var header = new byte[sizeof(int)];
            stream.ReadExactly(header);
            int requestLength = BinaryPrimitives.ReadInt32BigEndian(header);
            stream.ReadExactly(new byte[requestLength]);
            BinaryPrimitives.WriteInt32BigEndian(header, response.Length);
            stream.Write(header);
            stream.Write(response);
        });
}
