from . import *

# Tracking Prey

def GetAbilities() -> Sequence['Ability']:

    def tracking_prey_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        if player.IsAlterEgo():
            this.PlaceAccelerationToken(1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            tracking_prey_revealed
        ),
    ]

