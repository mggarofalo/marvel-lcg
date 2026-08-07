from . import *

# Build Support

def GetAbilities() -> Sequence['Ability']:

    def build_support(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        def action(player: 'Player'):
            face = Search.PlayerCard(
                effect,
                player,
                include_player_deck=True,
                include_discard_pile=True,
                card_type=Support,
                cost_equal_or_less=3,
                may=True,
            )
            if face:
                face.PutIntoPlay(player, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            build_support,
        ),
    ]

