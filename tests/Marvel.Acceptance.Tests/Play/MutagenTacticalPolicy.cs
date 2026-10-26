using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>A solo Mutagen strategy using public cards, the answering hand and engine offers.</summary>
/// <remarks>Priorities are authored tactics; the engine owns legality, costs and effects.</remarks>
internal sealed class MutagenTacticalPolicy(ICardFacts facts)
{
    private readonly CoreGamePolicy fallback = new(facts);

    internal int Answered { get; private set; }
    internal int Recoveries { get; private set; }
    internal int AllyDefenses { get; private set; }

    internal Decision Answer(Game game)
    {
        Answered++;
        var prompt = game.Pending!;
        if (prompt.Affordances.Any(option => option.Verb is Game.ResolveMulligans))
            return Mulligan(game);
        if (prompt.Affordances.Any(option => option.Verb is Game.EndPhaseVerb))
            return fallback.Answer(game);
        var identity = game.State.Seats[prompt.Player].IdentityCard;
        long health = DamagePlacement.Health(game.State, facts, identity) - identity.Damage;
        if (prompt.Asking == Question.Defender) return Defend(game);
        if (prompt.Asking == Question.TurnOption)
            return Turn(game, identity, health) ?? Decision.Decline;
        if (FuryDraw(game) is { } draw) return Take(game, draw);
        if (prompt.Asking == Question.Element) return Element(game);
        var response = prompt.Affordances.FirstOrDefault(option => IsUsefulResponse(game, option));
        return response is not null ? Take(game, response) : fallback.Answer(game);
    }

    private static Affordance? FuryDraw(Game game)
    {
        if (!game.Pending!.Label.StartsWith("01084", StringComparison.Ordinal)) return null;
        return game.Pending.Affordances.FirstOrDefault(option => option.IsLegal
            && (option.Verb == "draw" || option.Label == "draw" || option.Description == "Draw 3 cards"));
    }

    private Decision Element(Game game) =>
        Take(game, game.Pending!.Affordances.Where(option => option.IsLegal)
            .OrderByDescending(option => option.AnchorId >= 0 && option.AnchorId < game.State.Cards.Count
                ? TargetPriority(game, game.State.Cards[option.AnchorId]) : 0).First());

    private Decision Mulligan(Game game)
    {
        var mulligan = game.Pending!.Affordances.Single();
        return Decision.Take(mulligan.Id, game.State.Seats[game.Pending.Player].Hand.Cards
            .Where(card => facts.Title(card.FaceId) is not ("Agent 13" or "Super-Soldier Serum"
                or "Captain America's Shield" or "Energy" or "Strength" or "Genius"))
            .Select(card => card.ObjectId).ToArray(), []);
    }

    private Decision Defend(Game game)
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
        return block && prompt.Cancellable ? Decision.Decline : fallback.Answer(game);
    }

    private bool IsUsefulResponse(Game game, Affordance option) => option.IsLegal
        && option.AnchorId >= 0 && option.AnchorId < game.State.Cards.Count
        && facts.Title(game.State.Cards[option.AnchorId].FaceId) is "Shield Block"
            or "Nick Fury" or "Maria Hill" or "Squirrel Girl" or "Agent 13" or "Mockingbird" or "Hawkeye" or "Falcon" or "Quinjet"
        && MutagenPolicyPayment.Payment(option, game) is not null;

    private Decision? Turn(Game game, Card identity, long health)
    {
        bool hero = Forms.In(game.State, game.State.Seats[game.Pending!.Player], facts, Forms.Hero);
        var determination = NamedAction(game, "Fearless Determination");
        if (determination is not null) return Take(game, determination);
        var ready = NamedAction(game, "Captain America");
        var legal = game.Pending.Affordances.Where(option => option.IsLegal
            && MutagenPolicyPayment.Payment(option, game) is not null).ToArray();
        if (Essential(game, legal) is { } essential) return Take(game, essential);
        if (!hero && AlterEgoPower(game, identity) is { } preparation) return preparation;
        return ActiveTurn(game, hero, health, legal, ready);
    }

    private Affordance? NamedAction(Game game, string title) =>
        game.Pending!.Affordances.FirstOrDefault(option => option.IsLegal && option.Verb == Game.ActionVerb
            && facts.Title(game.State.Cards[option.AnchorId].FaceId) == title);

    private Affordance? Essential(Game game, IReadOnlyList<Affordance> legal)
    {
        var essential = legal.FirstOrDefault(option => option.Verb == CardPlay.Verb
            && facts.Title(game.State.Cards[option.AnchorId].FaceId) is "Super-Soldier Serum" or "Captain America's Shield");
        bool serumInPlay = game.State.Areas.Where(area => area.Type == DeckType.UpgradesArea)
            .SelectMany(area => area.Cards).Any(card => card.FaceId == "03010");
        bool preserveStrike = serumInPlay && legal.Any(option => option.Verb == Game.ActionVerb
            && facts.Title(game.State.Cards[option.AnchorId].FaceId) == "Heroic Strike");
        if (essential is not null && (facts.Title(game.State.Cards[essential.AnchorId].FaceId)
            != "Super-Soldier Serum" || !preserveStrike)) return essential;
        return null;
    }

    private Decision? AlterEgoPower(Game game, Card identity)
    {
        var recover = Find(game, BasicPowers.RecoverVerb);
        if (identity.Damage >= 3 && recover is not null)
        {
            Recoveries++;
            return Take(game, recover);
        }
        return Find(game, Game.ChangeForm) is { } change ? Take(game, change) : null;
    }

    private Decision? ActiveTurn(Game game, bool hero, long health, IReadOnlyList<Affordance> legal, Affordance? ready)
    {
        var action = BestAction(game, legal);
        var play = legal.Where(option => option.Verb == CardPlay.Verb)
            .OrderByDescending(option => PlayPriority(game, option)).FirstOrDefault();
        if (action is not null && ActionPriority(game, action) >= 90) return Take(game, action);
        if (play is not null && facts.Kind(game.State.Cards[play.AnchorId].FaceId) == CardKind.Ally) return Take(game, play);
        if (action is not null && health > 1 && ActionPriority(game, action) >= 10) return Take(game, action);
        if (play is not null) return Take(game, play);
        return RemainingPowers(game, hero, health, action, ready);
    }

    private Decision? RemainingPowers(Game game, bool hero, long health, Affordance? action, Affordance? ready)
    {
        if (BasicPower(game) is { } basic) return Take(game, basic);
        if (ready is not null && game.State.Seats[game.Pending!.Player].Hand.Cards
            .Any(card => facts.Title(card.FaceId) != "Captain America's Shield")) return Take(game, ready);
        if (hero && health <= 3 && Find(game, Game.ChangeForm) is { } retreat) return Take(game, retreat);
        return FinishTurn(game, hero, health, action);
    }

    private Affordance? BestAction(Game game, IEnumerable<Affordance> legal) =>
        legal.Where(option => option.Verb == Game.ActionVerb)
            .OrderByDescending(option => ActionPriority(game, option)).FirstOrDefault();

    private Decision? FinishTurn(Game game, bool hero, long health, Affordance? action)
    {
        var change = Find(game, Game.ChangeForm);
        if (!hero && health >= 7 && change is not null) return Take(game, change);
        if (BasicPower(game) is { } power) return Take(game, power);
        return action is not null && ActionPriority(game, action) >= 0 && health > 1 ? Take(game, action) : null;
    }

    private static Affordance? BasicPower(Game game)
    {
        long threat = game.State.TheCardIn(DeckType.MainSchemesArea)?.Tokens.GetValueOrDefault("k_threat") ?? 0;
        bool minions = game.State.Areas.Any(area => area.Type == DeckType.EngagedEnemiesArea && area.Cards.Count > 0);
        bool sideSchemes = !minions && game.State.Areas.Any(area => area.Type == DeckType.SideSchemesArea && area.Cards.Count > 0);
        string preferred = threat >= 3 || sideSchemes ? BasicPowers.ThwartVerb : BasicPowers.AttackVerb;
        return Find(game, preferred) ?? Find(game, BasicPowers.AttackVerb)
            ?? Find(game, BasicPowers.ThwartVerb);
    }

    private int ActionPriority(Game game, Affordance option)
    {
        string title = facts.Title(game.State.Cards[option.AnchorId].FaceId);
        return title switch
        {
            "Heroic Strike" or "Fearless Determination" => 100,
            "Shield Toss" => 110,
            "First Aid" => game.State.Seats[game.Pending!.Player].IdentityCard.Damage >= 2 ? 120 : -100,
            "Lead from the Front" => 80,
            "Captain America" => -100,
            "Steve's Apartment" or "Avengers Mansion" or "Helicarrier" => 150,
            _ => 10,
        };
    }

    private int PlayPriority(Game game, Affordance option) => facts.Title(game.State.Cards[option.AnchorId].FaceId) == "Super-Soldier Serum" ? 200 : facts.Kind(game.State.Cards[option.AnchorId].FaceId) switch
    {
        CardKind.Ally => 100,
        CardKind.Upgrade => 60,
        CardKind.Support => 40,
        _ => 10,
    };

    private Decision Take(Game game, Affordance option)
    {
        var payment = MutagenPolicyPayment.Payment(option, game) ?? [];
        var values = MutagenPolicyPayment.Values(option);
        return Decision.Take(option.Id, Targets(game, option), payment, values,
            MutagenPolicyPayment.Allocations(option, payment, values));
    }

    private IReadOnlyList<int> Targets(Game game, Affordance option)
    {
        var wanted = option.Targets;
        if (wanted is not null && !wanted.IsGrouped && option.AnchorId >= 0 && facts.Title(game.State.Cards[option.AnchorId].FaceId) == "Shield Toss")
        {
            int enemies = 1 + game.State.Areas.Where(area => area.Type == DeckType.EngagedEnemiesArea).Sum(area => area.Cards.Count);
            var discards = wanted.Legal.Select(id => game.State.Cards[id])
                .OrderBy(card => MutagenPolicyPayment.KeepValue(game, new ResourceSource(card.ObjectId, "")))
                .Take(Math.Min(enemies, wanted.Max)).Select(card => card.ObjectId).ToArray();
            if (wanted.Allows(discards)) return discards;
        }
        if (wanted is null || wanted.Min != 1 || wanted.IsGrouped)
            return PolicyTargetSelection.Select(game.State, option);
        var targets = wanted.Legal.Select(id => game.State.Cards[id]);
        var target = targets.OrderByDescending(card => TargetPriority(game, card))
            .FirstOrDefault(card => wanted.Allows([card.ObjectId]));
        return target is null ? PolicyTargetSelection.Select(game.State, option) : [target.ObjectId];
    }

    private long TargetPriority(Game game, Card card)
    {
        if (game.Pending!.Label.StartsWith("01086", StringComparison.Ordinal)) return card == game.State.Seats[game.Pending.Player].IdentityCard ? card.Damage * 100 : -100;
        if (game.State.Seats[game.Pending!.Player].Hand.Cards.Contains(card))
            return facts.Title(card.FaceId) is "Energy" or "Genius" or "Strength" ? -200 : -MutagenPolicyPayment.KeepValue(game, new ResourceSource(card.ObjectId, ""));
        return BoardTargetPriority(game, card);
    }

    private long BoardTargetPriority(Game game, Card card)
    {
        if (EnemyTargetPriority(game, card) is { } enemy) return enemy;
        var kind = facts.Kind(card.FaceId);
        if (kind == CardKind.EncounterSideScheme) return 500;
        if (kind == CardKind.MainScheme) return card.Tokens.GetValueOrDefault("k_threat") >= 5 ? 700 : 400;
        return FriendlyTargetPriority(game, card);
    }

    private long? EnemyTargetPriority(Game game, Card card)
    {
        var kind = facts.Kind(card.FaceId);
        bool heavyAttack = game.Pending!.Label.StartsWith("03004", StringComparison.Ordinal);
        if (kind == CardKind.EncounterVillain && game.Pending.Label.StartsWith("03020", StringComparison.Ordinal)) return 3000;
        if (kind == CardKind.EncounterVillain && heavyAttack) return 2000;
        if (kind == CardKind.Minion) return 1000 + (heavyAttack ? 1 : -1)
            * (DamagePlacement.Health(game.State, facts, card) - card.Damage);
        return null;
    }

    private long FriendlyTargetPriority(Game game, Card card)
    {
        if (card == game.State.Seats[game.Pending!.Player].IdentityCard) return card.Damage * 10;
        if (facts.Kind(card.FaceId) != CardKind.Ally) return 0;
        return facts.Title(card.FaceId) switch
        {
            "Nick Fury" => 800,
            "Maria Hill" => 700,
            "Agent 13" => 600,
            "Squirrel Girl" => 500,
            "Hawkeye" => 400,
            _ => card.Damage * 5,
        };
    }

    private static Affordance? Find(Game game, string verb) =>
        game.Pending!.Affordances.FirstOrDefault(option => option.IsLegal && option.Verb == verb);
}
