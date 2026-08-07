from . import *

# * Luminous

def GetAbilities() -> Sequence['Ability']:

    def luminous(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        faces = Worlds.DiscardEncounterCards(1, effect)
        value = FacesCounter.CountTotalBoostIcons(faces)
        if value >= 2:
            player.DealEncounterCards(1, effect)


    return [
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedInterrupt,
            "This",
            luminous,
        ),
    ]

