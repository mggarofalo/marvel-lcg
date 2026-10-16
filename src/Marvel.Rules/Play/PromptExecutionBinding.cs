using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>An engine-local decision address, rebuilt by replay and absent from protocol JSON.</summary>
internal sealed record PromptExecutionBinding(
    object Game, long Frame, Stage Stage, Occurrence? Window, WindowKind? Kind)
{
    internal static Prompt Bind(World world, Prompt prompt) => BindCore(world, prompt, null);

    internal static Prompt BindWindow(World world, Prompt prompt) =>
        BindCore(world, prompt, world.Windows.Current
            ?? throw new InvalidOperationException("a window prompt requires its open window"));

    private static Prompt BindCore(World world, Prompt prompt, Window? window) => prompt with
    {
        ExecutionBinding = new(world.Agenda.Executions.Identity,
            world.Agenda.Current?.FrameId ?? 0, world.Agenda.Stage,
            window?.Occurrence, window?.Kind),
    };

    internal static void RequireCurrent(World world, Prompt prompt)
    {
        if (prompt.ExecutionBinding is not { } binding)
            throw new ArgumentException("the prompt has no authoritative execution address", nameof(prompt));
        binding.RequireFrame(world.Agenda, prompt);
        binding.RequireWindow(world.Windows.Current, prompt);
    }

    private void RequireFrame(Agenda agenda, Prompt prompt)
    {
        if (!ReferenceEquals(Game, agenda.Executions.Identity)
            || (agenda.Current?.FrameId ?? 0) != Frame || agenda.Stage != Stage)
            throw new ArgumentException("the prompt does not belong to the current execution", nameof(prompt));
    }

    private void RequireWindow(Window? current, Prompt prompt)
    {
        if (Window is not null && (!ReferenceEquals(Window, current?.Occurrence) || Kind != current?.Kind))
            throw new ArgumentException("the prompt does not belong to the current window", nameof(prompt));
    }
}
