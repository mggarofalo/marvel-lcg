using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using static Marvel.Cards.Run.AbilityContinuationWireCodec;
using static Marvel.Cards.Run.AbilityEffectStructure;

namespace Marvel.Cards.Run;

internal sealed class ContinuationPathDecoder(
    AbilityEffect root, bool eachPlayerFrame, bool finalPlayer)
{
    private AbilityEffect node = root;
    private readonly ImmutableArray<AbilityStructuralFrame>.Builder frames =
        ImmutableArray.CreateBuilder<AbilityStructuralFrame>();
    private readonly ImmutableArray<AbilityContinuationFrame>.Builder facts =
        ImmutableArray.CreateBuilder<AbilityContinuationFrame>();

    internal (AbilityEffect Node, ImmutableArray<AbilityStructuralFrame> Frames,
        ImmutableArray<AbilityContinuationFrame> Facts) Decode(
            IReadOnlyList<string> path)
    {
        foreach (string encoded in path) DecodeFrame(encoded);
        return (node, frames.ToImmutable(), facts.ToImmutable());
    }

    private void DecodeFrame(string encoded)
    {
        var parts = encoded.Split(':');
        if (parts[0] == "choice")
        {
            DecodeChoice(parts, encoded);
            return;
        }
        switch (parts[0])
        {
            case "seq": DecodeSequence(parts, encoded); break;
            case "and": DecodeSimultaneous(parts, encoded); break;
            case "if": DecodeConditional(parts, encoded); break;
            case "then" or "otherwise": DecodeDependent(parts, encoded); break;
            case "defense":
                frames.Add(new DefenseFrame());
                node = EffectBody(node);
                break;
            case "eachPlayer": DecodeEachPlayer(encoded); break;
            case "forEach": DecodeForEach(parts, encoded); break;
            case "eachTime": DecodeEachTime(parts, encoded); break;
            default:
                throw new RulesNotImplementedException(
                    $"ability continuation frame '{encoded}' is not implemented");
        }
    }

    private void DecodeSequence(string[] parts, string encoded)
    {
        var sequence = node as AbilityEffect.Sequence ?? throw Invalid(encoded);
        int index = ParseIndex(parts, encoded);
        if (index < 0 || index >= sequence.Effects.Length) throw Invalid(encoded);
        frames.Add(new SequenceFrame(index + 1, sequence.Effects.Length));
        facts.Add(new SequenceContinuationFrame(sequence, index));
        node = sequence.Effects[index];
    }

    private void DecodeSimultaneous(string[] parts, string encoded)
    {
        var simultaneous = node as AbilityEffect.Simultaneous ?? throw Invalid(encoded);
        int current = ParseIndex(parts, encoded);
        var remaining = ValidRemaining(node, parts, encoded);
        var completed = Completed(parts, encoded);
        frames.Add(new SimultaneousFrame(current, [.. remaining], [.. completed]));
        facts.Add(new SimultaneousContinuationFrame(simultaneous, [.. remaining]));
        node = simultaneous.Effects.ElementAt(current);
    }

    private void DecodeConditional(string[] parts, string encoded)
    {
        bool then = parts.Length == 2 && parts[1] is "then" or "else"
            ? parts[1] == "then"
            : throw Invalid(encoded);
        var conditional = node as AbilityEffect.Conditional ?? throw Invalid(encoded);
        frames.Add(new ConditionalFrame(then));
        node = ConditionalBranch(conditional, then ? "then" : "else")
            ?? throw Invalid(encoded);
    }

    private void DecodeDependent(string[] parts, string encoded)
    {
        var dependent = node as AbilityEffect.Dependent ?? throw Invalid(encoded);
        bool onFull = parts[0] == "then";
        if (parts.Length >= 2 && parts[1] == "effect")
        {
            AbilityStructuralOutcome? outcome = parts.Length == 3
                ? ParseOptionalOutcome(parts[2], encoded)
                : throw Invalid(encoded);
            frames.Add(new DependentFrame(onFull, true, outcome));
            facts.Add(new DependentContinuationFrame(dependent, true, outcome));
            node = dependent.Effect;
            return;
        }
        if (parts.Length == 2 && parts[1] == parts[0])
        {
            frames.Add(new DependentFrame(onFull, false, null));
            node = ContinuationChild(dependent, parts[0]);
            return;
        }
        throw Invalid(encoded);
    }

    private void DecodeEachPlayer(string encoded)
    {
        var each = node as AbilityEffect.EachPlayer ?? throw Invalid(encoded);
        frames.Add(new EachPlayerFrame(-1, finalPlayer));
        facts.Add(new EachPlayerContinuationFrame(eachPlayerFrame && !finalPlayer));
        node = each.Effect;
    }

    private void DecodeForEach(string[] parts, string encoded)
    {
        var repeated = node as AbilityEffect.ForEach ?? throw Invalid(encoded);
        int current = ParseIndex(parts, encoded);
        long count = ParseForEachCount(parts, encoded);
        if (current < 0 || current >= count) throw Invalid(encoded);
        frames.Add(new ForEachFrame(current + 1, count));
        facts.Add(new ForEachContinuationFrame(repeated, current, count));
        node = repeated.Effect;
    }

    private void DecodeEachTime(string[] parts, string encoded)
    {
        var repeated = node as AbilityEffect.EachTime ?? throw Invalid(encoded);
        int current = ParseIndex(parts, encoded);
        long count = ParseForEachCount(parts, encoded);
        int card = ParseEachTimeCard(parts, encoded);
        if (current < 0 || current >= count) throw Invalid(encoded);
        frames.Add(new EachTimeFrame(current + 1, count, card));
        facts.Add(new EachTimeContinuationFrame(repeated, current, count));
        node = repeated.Then;
    }

    private void DecodeChoice(string[] parts, string encoded)
    {
        if (IsChoiceOption(parts))
        {
            DecodeChoiceOption(parts, encoded);
            return;
        }
        if (IsChoiceEffect(parts))
        {
            DecodeChoiceEffect(encoded);
            return;
        }
        if (IsChoiceOtherwise(parts))
        {
            frames.Add(new ChoiceOtherwiseFrame());
            node = EffectFollowing(node);
            return;
        }
        throw new RulesNotImplementedException(
            $"ability continuation frame '{encoded}' is not implemented");
    }

    private static bool IsChoiceOption(string[] parts) =>
        parts.Length == 3 && parts[1] == "option";

    private static bool IsChoiceEffect(string[] parts) =>
        parts.Length == 2 && parts[1] == "effect";

    private static bool IsChoiceOtherwise(string[] parts) =>
        parts.Length == 2 && parts[1] == "otherwise";

    private void DecodeChoiceOption(string[] parts, string encoded)
    {
        var options = node as AbilityEffect.Choose ?? throw Invalid(encoded);
        int option = ParseIndex(parts, encoded, 2);
        if (option < 0 || option >= options.Options.Length) throw Invalid(encoded);
        frames.Add(new ChoiceFrame(option, null));
        node = options.Options[option];
    }

    private void DecodeChoiceEffect(string encoded)
    {
        if (node is not AbilityEffect.ChooseCard choice) throw Invalid(encoded);
        frames.Add(new ChoiceFrame(null, null));
        node = choice.Effect;
    }
}
