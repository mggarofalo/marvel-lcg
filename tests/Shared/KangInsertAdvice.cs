using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Tests;

/// <summary>
/// The insert's tactical advice, answered before <c>CoreGamePolicy</c> while the table is separated.
/// </summary>
/// <remarks>
/// pack:mc11:game-areas: "If a player is in danger of being defeated by Kang (II), that player
/// should change to alter-ego form and recover hit points. It is better to let Kang (II)
/// complete his main scheme than to let Kang (II) defeat your hero." The threshold of half
/// printed hit points is our choice. It chooses only offered turn options.
/// </remarks>
internal static class KangInsertAdvice
{
    /// <summary>The policy's answer, after the insert's advice while the table is separated.</summary>
    internal static Decision Answer(CoreGamePolicy policy, Game game) => Prefer(game, game.Pending!) ?? policy.Answer(game);

    /// <summary>A turn answer that follows the advice, or null to let the research policy answer.</summary>
    internal static Decision? Prefer(Game game, Prompt prompt)
    {
        var world = game.State;
        if (!Places.Separated(world) || game.Phase != GamePhase.PlayerTurn || !game.IsRootPrompt
            || prompt.Asking != Question.TurnOption) return null;
        var seat = world.Seats[prompt.Player];
        long health = DamagePlacement.Health(world, world.Facts, seat.IdentityCard);
        bool endangered = 2 * seat.IdentityCard.Damage >= health;
        bool hero = Forms.In(world, seat, world.Facts, Forms.Hero);
        if (hero)
            return endangered && Offer(prompt, Game.ChangeForm) is { } change ? Decision.Take(change.Id) : null;
        if (seat.IdentityCard.Damage == 0) return null;
        return Offer(prompt, BasicPowers.RecoverVerb) is { } recover ? Decision.Take(recover.Id) : Decision.Decline;
    }

    private static Affordance? Offer(Prompt prompt, string verb) => prompt.Affordances.FirstOrDefault(
        option => option.IsLegal && string.Equals(option.Verb, verb, StringComparison.Ordinal));
}
