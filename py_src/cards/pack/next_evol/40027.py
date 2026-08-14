from . import *

# Build Support

def GetAbilities() -> Sequence['Ability']:

    def build_support(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        def action(player: 'Player'):
            # `Search.PlayerCard(..., may=True)` is the obvious spelling of
            # "each player *may* search" and it does not work: `may` widens the
            # selector to (0, max), `EffectChecker.UpdateLegalTargets` then
            # reports a target range whose minimum and maximum are both 0, and
            # `PlayerAction.ChoiceAndSpellEffect` auto-resolves the choice with
            # no targets rather than asking. The search silently finds nothing.
            # The opt-in is spelled as an explicit `MayChooseOneAbility` around
            # a mandatory search instead, which is what 51026 -- the reprint of
            # this card -- already did.
            def search(targets: Sequence['CardFace']):
                face = Search.PlayerCard(
                    effect,
                    player,
                    include_player_deck=True,
                    include_discard_pile=True,
                    card_type=Support,
                    cost_equal_or_less=3,
                )
                if face:
                    face.PutIntoPlay(player, effect)

            player.MayChooseOneAbility(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Search their deck and discard pile for a support with a cost of 3 or less and put it into play",
                    search
                )
            )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            build_support,
        ),
    ]

