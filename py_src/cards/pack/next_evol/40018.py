from . import *

# Call for Backup

def GetAbilities() -> Sequence['Ability']:

    def call_for_backup(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        def action(player: 'Player'):
            face = Search.PlayerCard(
                effect,
                player,
                include_player_deck=True,
                include_discard_pile=True,
                card_type=Ally,
                may=True,
            )
            if face:
                face.PutIntoPlay(player, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            call_for_backup,
        ),
    ]

