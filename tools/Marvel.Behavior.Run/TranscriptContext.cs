using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Behavior.Run;

internal sealed class TranscriptContext
{
    public TranscriptContext(
        string obligation,
        SetupCatalog setup,
        CardCatalog cards,
        AbilityBook abilities)
    {
        Obligation = obligation;
        Setup = setup;
        Cards = cards;
        Abilities = abilities;
    }

    public string Obligation { get; }

    public SetupCatalog Setup { get; }

    public CardCatalog Cards { get; }

    public AbilityBook Abilities { get; }

    public CanonicalCoreScene? Scene { get; set; }

    public List<GameEvent> Events { get; } = [];

    public string CurrentPrompt { get; set; } = "<none>";

    public string? ExpectedException { get; set; }

    public RulesNotImplementedException? PendingException { get; set; }

    public string? ExceptionDigest { get; set; }

    public bool ExceptionObserved { get; set; }

    public (int Seat, string From, string To)? LastFormChange { get; set; }

    public IReadOnlySet<int>? LastCardOptions { get; set; }

    public bool? LastAvailability { get; set; }

    public string? LastInspectedFace { get; set; }

    public (int Card, string Resources)? LastResourceGeneration { get; set; }

    public Prompt? PendingPrompt { get; set; }

    public Game? Game { get; set; }

    public (int Seat, IReadOnlyList<int> Unshuffled, IReadOnlyList<int> After)?
        LastSetupDeckShuffle { get; set; }

    public World World => Scene?.World
        ?? throw new TranscriptException("a canonical Core scene has not been constructed");
}
