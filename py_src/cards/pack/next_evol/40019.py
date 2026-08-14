from . import *

# * Lock and Load

def GetAbilities() -> Sequence['Ability']:

    def lock_and_load(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        def action(player: 'Player'):
            face = Search.PlayerCard(
                effect,
                player,
                include_player_deck=True,
                include_discard_pile=True,
                trait="WEAPON",
                card_type=Upgrade,
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
            lock_and_load,
        ),
    ]

