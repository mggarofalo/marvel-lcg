from . import *

# Doctor Octopus

def GetAbilities() -> Sequence['Ability']:

    def doctor_octopus(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        this.PlaceThreatOnSchemes("EachScheme", 1, effect)


    return [
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            doctor_octopus,
        ),
    ]

