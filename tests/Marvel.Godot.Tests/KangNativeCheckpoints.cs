using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Tests;

namespace Marvel.Godot.Tests;

/// <summary>
/// Carries a Kang native journey from one observed table state to the next with the insert-advised
/// research policy, through the same host and capability the native client uses.
/// </summary>
/// <remarks>
/// The game opens at the first checkpoint (<see cref="KangNativeScene"/>). The client reaches each later
/// checkpoint by pressing Synchronize; the host answers every intermediate prompt before serving that
/// request. The join prompt is answered by the client.
/// </remarks>
internal sealed class KangNativeCheckpoints(string contract, KangRecipeFactory factory)
{
    private readonly Queue<(string Name, Func<Game, bool> Reached)> remaining = new(Plan(contract));
    private CoreGamePolicy? policy;

    internal List<string> Reached { get; } = [];

    /// <summary>Answers prompts until the next checkpoint holds, and returns the last authoritative response.</summary>
    internal EngineResponse Advance(EngineHost host, EngineResponse current, string capability)
    {
        if (remaining.Count == 0) return current;
        (string name, Func<Game, bool> reached) = remaining.Dequeue();
        policy ??= new CoreGamePolicy(factory.Game.State.Facts);
        EngineResponse latest = current;
        for (int answer = 0; !reached(factory.Game); answer++)
        {
            if (factory.Game.Pending is null || answer >= 400)
                throw new InvalidOperationException($"The Kang journey never reached its {name} checkpoint.");
            latest = host.Exchange(EngineRequest.ResolveGame($"kang-{name}-{answer}", latest.GameId,
                capability, KangInsertAdvice.Answer(policy, factory.Game), latest.Revision));
            if (latest.Error is { } error) throw new InvalidOperationException($"{name}: {error}");
        }
        Reached.Add(name);
        return latest;
    }

    /// <summary>The state a contract's game opens at, which <see cref="KangNativeScene"/> plays to.</summary>
    internal static Func<Game, bool> Opening(string contract) => Plan(contract).First().Item2;

    private static IEnumerable<(string, Func<Game, bool>)> Plan(string contract) => contract switch
    {
        "kang-2" =>
        [
            ("split", game => Split(game, 2) && game.Pending?.PublicKind == PublicDecisionKind.PlayerAction),
            ("acting", game => Separated(game) && KangTable.Of(game.State)?.Acting is not null),
            ("departure", game => Separated(game) && game.State.Agenda.KangTransitions.Pending.Count > 0),
            ("join", game => game.Pending?.Trigger == KangJoinOperation.Join),
            ("reunion", Reunited),
        ],
        "kang-3" => [("split", game => Split(game, 3))],
        "kang-4" => [("split", game => Split(game, 4)), ("retained", Retained)],
        _ => [],
    };

    // Each journey opens once every player has their own game area.
    private static bool Split(Game game, int players) => Separated(game) && LocalAreas(game) == players;

    private static bool Reunited(Game game) =>
        KangTable.Of(game.State)?.Stage == 3 && game.Pending?.PublicKind == PublicDecisionKind.PlayerAction;

    private static bool Retained(Game game) => GameAreaFacts.Of(game.State).Any(area => area.Retained);

    private static int LocalAreas(Game game) => GameAreaFacts.Of(game.State).Count(area => !area.Central);

    private static bool Separated(Game game) => Places.Separated(game.State);
}
