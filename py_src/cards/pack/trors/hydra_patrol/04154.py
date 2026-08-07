from . import *

# Hydra Patrol

def GetAbilities() -> Sequence['Ability']:

    def hydra_patrol(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                trait="HYDRA",
                card_type=Minion
            )
            if face:
                face.PutIntoPlay(player, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            hydra_patrol
        )
    ]

