from . import *

# * Build Support

def GetAbilities() -> Sequence['Ability']:

    def build_support(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        def action(player: 'Player'):
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

