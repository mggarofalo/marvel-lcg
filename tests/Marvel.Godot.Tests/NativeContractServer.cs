using System.Net;
using System.Net.Sockets;
using Marvel.Protocol;
using Marvel.Server;
using Marvel.Transport;
using Xunit;

namespace Marvel.Godot.Tests;

internal sealed class NativeContractServer : IAsyncDisposable
{
    private TcpListener listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource stopping = new();
    private readonly EngineHost host;
    private readonly NativeSceneGameFactory factory;
    private readonly string contract;
    private readonly string directory;
    private readonly Task serving;
    private bool faultUsed;

    internal NativeContractServer(string contract, string directory)
    {
        this.contract = contract;
        this.directory = directory;
        factory = new NativeSceneGameFactory(contract);
        host = new EngineHost(factory);
        listener.Start();
        Endpoint = $"tcp://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}";
        serving = ServeAsync();
    }

    internal string Endpoint { get; }
    internal List<EngineRequest> Requests { get; } = [];
    internal List<EngineResponse> Responses { get; } = [];
    internal Task Completion => serving;
    internal Exception? SceneFailure => factory.Failure;

    private async Task ServeAsync()
    {
        try
        {
            while (!stopping.IsCancellationRequested)
            {
                using TcpClient peer = await listener.AcceptTcpClientAsync(stopping.Token);
                await ServePeerAsync(peer);
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
    }

    private async Task ServePeerAsync(TcpClient peer)
    {
        NetworkStream stream = peer.GetStream();
        byte[]? bytes = await SocketFrame.ReadAsync(stream, stopping.Token);
        if (bytes is null) return;
        EngineRequest request = EngineJson.ReadRequest(bytes);
        EngineResponse response = AuthoritativeResponse(request);
        if (LoseCommittedReply(request)) return;
        if (contract == "lost-reply" && faultUsed && request.Operation == EngineProtocol.Sync)
            await WaitForSignalAsync("recover");
        await SocketFrame.WriteAsync(stream, EngineJson.Write(response), stopping.Token);
        if (contract == "not-sent" && !faultUsed && request.Operation == EngineProtocol.Open)
        {
            faultUsed = true;
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            await File.WriteAllTextAsync(Path.Combine(directory, "disconnected"), "ready", stopping.Token);
            await WaitForSignalAsync("retry");
            listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            await File.WriteAllTextAsync(Path.Combine(directory, "listening"), "ready", stopping.Token);
        }
    }

    private EngineResponse AuthoritativeResponse(EngineRequest request)
    {
        Requests.Add(request);
        EngineResponse response = host.Exchange(request);
        if (contract == "repeated-targets" && request.Operation == EngineProtocol.Open)
            response = PrepareExplosion(response);
        Responses.Add(response);
        return response;
    }

    private EngineResponse PrepareExplosion(EngineResponse opening)
    {
        EngineResponse current = opening;
        for (int answer = 0; answer < 30; answer++)
        {
            Assert.Null(current.Error);
            if (current.Prompt!.Affordances.Any(offer => offer.Targets is { AllowRepeated: true, Min: 3, Max: 3 }))
                return current with { RequestId = opening.RequestId, Capability = opening.Capability, Invitations = opening.Invitations };
            current = host.Exchange(EngineRequest.ResolveGame("scene-prelude-" + answer,
                opening.GameId, opening.Capability!, factory.PreludeAnswer(), current.Revision));
        }
        throw new InvalidOperationException("The recorded Core prelude did not reach Explosion's three-point allocation.");
    }

    private bool LoseCommittedReply(EngineRequest request)
    {
        if (contract != "lost-reply" || faultUsed || request.Operation != EngineProtocol.Resolve) return false;
        faultUsed = true;
        File.WriteAllText(Path.Combine(directory, "committed"), "ready");
        return true;
    }

    private async Task WaitForSignalAsync(string signal)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        while (!File.Exists(Path.Combine(directory, signal))) await Task.Delay(10, deadline.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await stopping.CancelAsync();
        try { await serving; }
        finally { listener.Stop(); stopping.Dispose(); }
    }
}
