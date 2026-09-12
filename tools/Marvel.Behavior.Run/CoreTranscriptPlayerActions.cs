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

internal static class CoreTranscriptPlayerActions
{
    internal static void VillainDamagesCard(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card villain = context.World.TheCardIn(DeckType.VillainArea)
            ?? throw new TranscriptException($"{step.Location}: no villain is in play");
        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        _ = DamagePlacement.Deal(
            context.World,
            context.Cards,
            villain,
            target,
            Number(match, "count", step),
            "behavioral transcript",
            "Damage",
            context.Events);
    }

    internal static void IdentityDefeated(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Elimination.Eliminate(
            context.World,
            context.Cards,
            Seat(match, step),
            "behavioral transcript",
            context.Events);
    }

    internal static void ChangeForm(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        int seatIndex = Seat(match, step);
        Seat seat = context.World.Seats[seatIndex];
        string from = FormName(Forms.Of(context.World, seat, context.Cards));
        _ = Forms.ChangeAndSchedule(context.World, seat, context.Cards, round: 1);
        string to = FormName(Forms.Of(context.World, seat, context.Cards));
        context.LastFormChange = (seatIndex, from, to);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    internal static void InflictStatus(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        string status = match.Groups["action"].Value == "stuns"
            ? Statuses.Stunned
            : Statuses.Confused;
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        _ = Statuses.Inflict(context.World, context.Cards, target, status);
    }

    internal static void CardDealsDamage(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card source = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["source"].Value,
            Number(match, "sourceCopy", step)));
        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        _ = DamagePlacement.Deal(
            context.World,
            context.Cards,
            source,
            target,
            Number(match, "count", step),
            "behavioral transcript",
            "Damage",
            context.Events);
    }

    internal static void PlaceThreat(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card scheme = context.SceneRequired(step).Find(SceneCard(match, step));
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Threat.Place(
            context.World,
            context.Cards,
            context.World.Abilities,
            scheme,
            Number(match, "count", step),
            "behavioral transcript",
            context.Events);
        FinishAgenda(context, step);
    }

    internal static void RemoveThreat(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        Card scheme = context.SceneRequired(step).Find(SceneCard(match, step));
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.World.Agenda.Add(new PhaseStep(
            Steps.DealAttackDamage, Round: 1, Number: 4, Plan: true));
        _ = context.World.Agenda.Begin(context.World, context.Cards);
        _ = Threat.Remove(
            context.World,
            context.Cards,
            context.World.Abilities,
            scheme,
            Number(match, "count", step),
            "behavioral transcript",
            "Remove_Threat",
            context.Events);
        FinishAgenda(context, step);
    }

    internal static void BasicAttack(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        BasicPowerInitiation.BasicAttack(
            context.World,
            context.Cards,
            Seat(match, step),
            context.SceneRequired(step).Find(SceneCard(match, step)),
            context.Events);
        FinishAgenda(context, step);
    }

    internal static void BeginBasicAttack(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        BasicPowerInitiation.BasicAttack(
            context.World,
            context.Cards,
            Seat(match, step),
            context.SceneRequired(step).Find(SceneCard(match, step)),
            context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    internal static void BasicAttackAcceptingWithPayment(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        BasicPowerInitiation.BasicAttack(
            context.World,
            context.Cards,
            Seat(match, step),
            context.SceneRequired(step).Find(SceneCard(match, step)),
            context.Events);
        Card payment = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["payment"].Value,
            Number(match, "paymentCopy", step)));
        Card target = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["target"].Value,
            Number(match, "targetCopy", step)));
        FinishAgendaAccepting(
            context, step, match.Groups["label"].Value, payment.ObjectId, target.ObjectId);
    }

    internal static void BasicAttackAcceptingWithPaymentWithoutTarget(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        BasicPowerInitiation.BasicAttack(
            context.World,
            context.Cards,
            Seat(match, step),
            context.SceneRequired(step).Find(SceneCard(match, step)),
            context.Events);
        Card payment = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["payment"].Value,
            Number(match, "paymentCopy", step)));
        FinishAgendaAccepting(
            context, step, match.Groups["label"].Value, payment.ObjectId);
    }

    internal static void BasicThwart(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        BasicThwartPowers.BasicThwart(
            context.World,
            context.Cards,
            Seat(match, step),
            context.SceneRequired(step).Find(SceneCard(match, step)),
            context.Events);
        FinishAgenda(context, step);
    }

    internal static void BasicRecovery(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        AllyBasicPowers.BasicRecovery(
            context.World, context.Cards, Seat(match, step), context.Events);
    }

    internal static void AllyPower(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card ally = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["ally"].Value,
            Number(match, "allyCopy", step)));
        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        string verb = match.Groups["power"].Value == "attack"
            ? BasicPowers.AttackVerb
            : BasicPowers.ThwartVerb;
        AllyBasicPowers.AllyPower(context.World, context.Cards, ally, target, verb, context.Events);
        FinishAgenda(context, step);
    }

    internal static void BeginAllyPower(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card ally = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["ally"].Value,
            Number(match, "allyCopy", step)));
        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        string verb = match.Groups["power"].Value == "attack"
            ? BasicPowers.AttackVerb
            : BasicPowers.ThwartVerb;
        AllyBasicPowers.AllyPower(context.World, context.Cards, ally, target, verb, context.Events);
        SetPendingPrompt(context, Sequence.Work(
            context.World, context.Cards, context.World.Abilities, context.Events));
    }

    internal static void AllyPowerAccepting(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        Card ally = context.SceneRequired(step).Find(new SceneCard(
            match.Groups["ally"].Value,
            Number(match, "allyCopy", step)));
        Card target = context.SceneRequired(step).Find(SceneCard(match, step));
        string verb = match.Groups["power"].Value == "attack"
            ? BasicPowers.AttackVerb
            : BasicPowers.ThwartVerb;
        AllyBasicPowers.AllyPower(context.World, context.Cards, ally, target, verb, context.Events);
        FinishAgendaAccepting(context, step, match.Groups["label"].Value);
    }

    internal static void BasicAttackTargets(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.LastAvailability = null;
        context.LastCardOptions = BasicPowers.Attackable(
                context.World, context.Cards, Seat(match, step))
            .Select(card => card.ObjectId)
            .ToHashSet();
    }

    internal static void BasicThwartTargets(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.LastAvailability = null;
        context.LastCardOptions = BasicPowers.Thwartable(
                context.World, context.Cards, Seat(match, step))
            .Select(card => card.ObjectId)
            .ToHashSet();
    }

    internal static void BasicRecoveryAvailability(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        context.Events.Clear();
        context.CurrentPrompt = "<none>";
        context.LastCardOptions = null;
        context.LastAvailability = BasicPowers.CanRecover(
            context.World, context.Cards, Seat(match, step));
    }

}
