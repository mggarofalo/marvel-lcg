using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>A public-board strategy; every action and payment is an engine offer.</summary>
internal sealed class GoblinTacticalPolicy(ICardFacts facts)
{
    private readonly CoreGamePolicy fallback = new(facts);

    internal int Answered { get; private set; }
    internal int Recoveries { get; private set; }
    internal int AllyDefenses { get; private set; }

    internal Decision Answer(Game game)
    {
        Answered++;
        var prompt = game.Pending!;
        if (prompt.Affordances.Any(option => option.Verb is Game.ResolveMulligans or Game.EndPhaseVerb))
            return fallback.Answer(game);
        var identity = game.State.Seats[prompt.Player].IdentityCard;
        long health = DamagePlacement.Health(game.State, facts, identity) - identity.Damage;
        if (prompt.Asking == Question.Defender) return Defend(game, health);
        if (prompt.Asking == Question.TurnOption)
            return Turn(game, identity, health) ?? Decision.Decline;
        if (prompt.Asking == Question.Element)
            return Take(game, prompt.Affordances.Where(option => option.IsLegal)
                .OrderByDescending(option => option.AnchorId >= 0 && option.AnchorId < game.State.Cards.Count
                    ? TargetPriority(game, game.State.Cards[option.AnchorId]) : 0).First());
        var response = prompt.Affordances.FirstOrDefault(option => IsUsefulResponse(game, option));
        return response is not null ? Take(game, response) : fallback.Answer(game);
    }

    private Decision Defend(Game game, long health)
    {
        var prompt = game.Pending!;
        var ally = prompt.Affordances.FirstOrDefault(option => option.IsLegal
            && facts.Kind(game.State.Cards[option.AnchorId].FaceId) == CardKind.Ally);
        if (ally is not null)
        {
            AllyDefenses++;
            return Take(game, ally);
        }
        bool block = game.State.Seats[prompt.Player].Hand.Cards
            .Any(card => facts.Title(card.FaceId) == "Shield Block");
        return (health > 8 || block) && prompt.Cancellable ? Decision.Decline : fallback.Answer(game);
    }

    private bool IsUsefulResponse(Game game, Affordance option) => option.IsLegal
        && option.AnchorId >= 0 && option.AnchorId < game.State.Cards.Count
        && facts.Title(game.State.Cards[option.AnchorId].FaceId) is "Backflip" or "Enhanced Spider-Sense" or "Shield Block"
            or "Nick Fury" or "Maria Hill" or "Squirrel Girl" or "Agent 13" or "Mockingbird"
        && GoblinPolicyPayment.Payment(option, game) is not null;

    private Decision? Turn(Game game, Card identity, long health)
    {
        bool hero = Forms.In(game.State, game.State.Seats[game.Pending!.Player], facts, Forms.Hero);
        bool building = BuildingBeforeFlip(game);
        if (RecoveryOrRetreat(game, identity, hero, health) is { } recovery) return recovery;
        if (NormanBasicAttack(game, building) is { } attack) return attack;
        var legal = game.Pending.Affordances.Where(option => option.IsLegal
            && GoblinPolicyPayment.Payment(option, game) is not null).ToArray();
        var action = BestAction(game, legal, building);
        if (action is not null && health > 1 && ActionPriority(game, action) >= 10) return Take(game, action);
        var play = legal.Where(option => option.Verb == CardPlay.Verb)
            .OrderByDescending(option => PlayPriority(game, option)).FirstOrDefault();
        if (play is not null) return Take(game, play);
        return FinishTurn(game, hero, health, building, action);
    }

    private Decision? RecoveryOrRetreat(Game game, Card identity, bool hero, long health)
    {
        var recover = Find(game, BasicPowers.RecoverVerb);
        if (!hero && identity.Damage >= 3 && recover is not null)
        {
            Recoveries++;
            return Take(game, recover);
        }
        var change = Find(game, Game.ChangeForm);
        return hero && health <= 6 && change is not null ? Take(game, change) : null;
    }

    private Decision? NormanBasicAttack(Game game, bool building)
    {
        var villain = game.State.TheCardIn(DeckType.VillainArea);
        if (!building && villain is not null && facts.Title(villain.FaceId) == "Norman Osborn"
            && Find(game, BasicPowers.AttackVerb) is { } basic) return Take(game, basic);
        return null;
    }

    private Affordance? BestAction(Game game, IEnumerable<Affordance> legal, bool building) =>
        legal.Where(option => option.Verb == Game.ActionVerb
            && (!building || facts.Title(game.State.Cards[option.AnchorId].FaceId)
                is not ("Heroic Strike" or "Shield Toss")))
            .OrderByDescending(option => ActionPriority(game, option)).FirstOrDefault();

    private Decision? FinishTurn(Game game, bool hero, long health, bool building, Affordance? action)
    {
        var change = Find(game, Game.ChangeForm);
        if (!hero && health >= 9 && change is not null) return Take(game, change);
        if (BasicPower(game, building) is { } power) return Take(game, power);
        return action is not null && health > 1 ? Take(game, action) : null;
    }

    private static Affordance? BasicPower(Game game, bool building)
    {
        long threat = game.State.TheCardIn(DeckType.MainSchemesArea)?.Tokens.GetValueOrDefault("k_threat") ?? 0;
        bool sideSchemes = game.State.Areas.Any(area => area.Type == DeckType.SideSchemesArea && area.Cards.Count > 0);
        string preferred = building || threat >= 5 || sideSchemes ? BasicPowers.ThwartVerb : BasicPowers.AttackVerb;
        return Find(game, preferred) ?? (building ? null : Find(game, BasicPowers.AttackVerb))
            ?? Find(game, BasicPowers.ThwartVerb);
    }

    private int ActionPriority(Game game, Affordance option)
    {
        string title = facts.Title(game.State.Cards[option.AnchorId].FaceId);
        return title switch
        {
            "Swinging Web Kick" or "Heroic Strike" or "Fearless Determination" => 100,
            "For Justice!" => 90,
            "Backflip" => 80,
            "Avengers Assemble!" => 0,
            "Strength In Numbers" => 0,
            "Captain America" => 0,
            _ => 10,
        };
    }

    private bool BuildingBeforeFlip(Game game) => game.Round <= 3
        && game.State.TheCardIn(DeckType.VillainArea) is { } villain
        && facts.Title(villain.FaceId) == "Norman Osborn";

    private int PlayPriority(Game game, Affordance option) => facts.Title(game.State.Cards[option.AnchorId].FaceId) == "Super-Soldier Serum" ? 200 : facts.Kind(game.State.Cards[option.AnchorId].FaceId) switch
    {
        CardKind.Ally => 100,
        CardKind.Upgrade => 60,
        CardKind.Support => 40,
        _ => 10,
    };

    private Decision Take(Game game, Affordance option)
    {
        var payment = GoblinPolicyPayment.Payment(option, game) ?? [];
        var values = GoblinPolicyPayment.Values(option);
        return Decision.Take(option.Id, Targets(game, option), payment, values,
            GoblinPolicyPayment.Allocations(option, payment, values));
    }

    private IReadOnlyList<int> Targets(Game game, Affordance option)
    {
        var wanted = option.Targets;
        if (wanted is null || wanted.Min != 1 || wanted.IsGrouped)
            return PolicyTargetSelection.Select(game.State, option);
        var targets = wanted.Legal.Select(id => game.State.Cards[id]);
        var target = targets.OrderByDescending(card => TargetPriority(game, card))
            .FirstOrDefault(card => wanted.Allows([card.ObjectId]));
        return target is null ? PolicyTargetSelection.Select(game.State, option) : [target.ObjectId];
    }

    private long TargetPriority(Game game, Card card)
    {
        if (game.State.Seats[game.Pending!.Player].Hand.Cards.Contains(card))
            return -GoblinPolicyPayment.KeepValue(game, new ResourceSource(card.ObjectId, ""));
        var kind = facts.Kind(card.FaceId);
        bool heavyAttack = game.Pending!.Label.StartsWith("03004", StringComparison.Ordinal)
            || game.Pending.Label.StartsWith("01005", StringComparison.Ordinal);
        if (kind == CardKind.EncounterVillain && heavyAttack) return 2000;
        if (kind == CardKind.Minion) return 1000 - (DamagePlacement.Health(game.State, facts, card) - card.Damage);
        if (kind == CardKind.EncounterSideScheme) return 500;
        if (kind == CardKind.MainScheme) return 400;
        return 0;
    }

    private static Affordance? Find(Game game, string verb) =>
        game.Pending!.Affordances.FirstOrDefault(option => option.IsLegal && option.Verb == verb);
}
