from . import *

# * The Lizard

def GetAbilities() -> Sequence['Ability']:

    def the_lizard(effect: 'Effect', message: 'Message.WhenPhaseBegin') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        this.HealHealth(1, effect)

    return [
        AbilityFactory.WhenPhaseBegin(
            AbilityType.ForcedInterrupt,
            "Villain",
            the_lizard
        ),
    ]

