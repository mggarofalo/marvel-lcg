from . import *

# Regenerative Research

def GetAbilities() -> Sequence['Ability']:

    def regenerative_research(effect: 'Effect', message: 'Message.WhenPhaseBegin') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.HealthUnits(Worlds.GetOnFieldEnemies(effect), 1, effect)


    return [
        AbilityFactory.WhenPhaseBegin(
            AbilityType.ForcedInterrupt,
            "Villain",
            regenerative_research
        ),
    ]

