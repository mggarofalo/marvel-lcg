using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Authored Thor tactics using the answering hand, public board and engine offers.</summary>
internal sealed class ThorTacticalPolicy(ICardFacts facts)
{
    private readonly CoreGamePolicy fallback = new(facts);
    internal int Answered { get; private set; }

    internal Decision Answer(Game game)
    {
        Answered++;
        var prompt = game.Pending!;
        if (prompt.Affordances.Any(option => option.Verb is Game.ResolveMulligans or Game.EndPhaseVerb))
            return fallback.Answer(game);
        if (prompt.Asking == Question.TurnOption)
            return Turn(game) ?? Decision.Decline;
        if (Defend(game) is { } defense) return defense;
        var response = UsefulResponse(game);
        return response is not null ? Take(game, response) : fallback.Answer(game);
    }

    private Decision? Defend(Game game)
    {
        var prompt = game.Pending!;
        if (prompt.Asking == Question.Defender && prompt.Cancellable)
        {
            var ally = prompt.Affordances.FirstOrDefault(option => option.IsLegal
                && facts.Kind(game.State.Cards[option.AnchorId].FaceId) == CardKind.Ally);
            if (ally is not null) return Take(game, ally);
            var identity = game.State.Seats[prompt.Player].IdentityCard;
            if (DamagePlacement.Health(game.State, facts, identity) - identity.Damage > 6)
                return Decision.Decline;
        }
        return null;
    }

    private Affordance? UsefulResponse(Game game) => game.Pending!.Affordances.FirstOrDefault(option => option.IsLegal
            && option.Timing is not null && option.AnchorId >= 0 && option.AnchorId < game.State.Cards.Count
            && facts.Title(game.State.Cards[option.AnchorId].FaceId) is "Thor" or "Lady Sif" or "Valkyrie"
                or "Chase Them Down" or "Battle Fury" or "Hall of Heroes"
            && GoblinPolicyPayment.Payment(option, game) is not null);

    private Decision? Turn(Game game)
    {
        var prompt = game.Pending!;
        var seat = game.State.Seats[prompt.Player];
        bool hero = Forms.In(game.State, seat, facts, Forms.Hero);
        long health = DamagePlacement.Health(game.State, facts, seat.IdentityCard) - seat.IdentityCard.Damage;
        var legal = prompt.Affordances.Where(option => option.IsLegal
            && GoblinPolicyPayment.Payment(option, game) is not null).ToArray();
        return Recovery(game, legal, hero, health) ?? ActionOrPower(game, legal, hero, health);
    }

    private static Decision? Recovery(Game game, Affordance[] legal, bool hero, long health)
    {
        var seat = game.State.Seats[game.Pending!.Player];
        var change = legal.FirstOrDefault(option => option.Verb == Game.ChangeForm);
        var recover = legal.FirstOrDefault(option => option.Verb == BasicPowers.RecoverVerb);
        if (!hero && recover is not null && seat.IdentityCard.Damage >= 4) return Take(game, recover);
        if (hero && health <= 4 && change is not null) return Take(game, change);
        return null;
    }

    private Decision? ActionOrPower(Game game, Affordance[] legal, bool hero, long health)
    {
        var action = legal.Where(option => option.Verb is Game.ActionVerb or CardPlay.Verb)
            .OrderByDescending(option => Priority(game, option)).FirstOrDefault();
        if (action is not null && health > 1) return Take(game, action);
        var change = legal.FirstOrDefault(option => option.Verb == Game.ChangeForm);
        if (!hero && change is not null) return Take(game, change);
        return BasicPower(game, legal);
    }

    private static Decision? BasicPower(Game game, Affordance[] legal)
    {
        long threat = game.State.TheCardIn(DeckType.MainSchemesArea)?.Tokens.GetValueOrDefault("k_threat") ?? 0;
        string power = threat >= 5 ? BasicPowers.ThwartVerb : BasicPowers.AttackVerb;
        var basic = legal.FirstOrDefault(option => option.Verb == power)
            ?? legal.FirstOrDefault(option => option.Verb == BasicPowers.AttackVerb)
            ?? legal.FirstOrDefault(option => option.Verb == BasicPowers.ThwartVerb);
        return basic is null ? null : Take(game, basic);
    }

    private int Priority(Game game, Affordance option) => facts.Title(game.State.Cards[option.AnchorId].FaceId) switch
    {
        "Thor" => 200,
        "Mjolnir" => 190,
        "Asgard" => 180,
        "God of Thunder" => 170,
        "Defender of the Nine Realms" => 160,
        "Hammer Throw" => 150,
        "Lightning Strike" => 140,
        _ => facts.Kind(game.State.Cards[option.AnchorId].FaceId) == CardKind.Ally ? 100 : 50,
    };

    private static Decision Take(Game game, Affordance option)
    {
        var payment = GoblinPolicyPayment.Payment(option, game) ?? [];
        var values = GoblinPolicyPayment.Values(option);
        return Decision.Take(option.Id, PolicyTargetSelection.Select(game.State, option), payment, values,
            GoblinPolicyPayment.Allocations(option, payment, values));
    }
}
