from . import *

# * Red Dagger

def GetAbilities() -> Sequence['Ability']:

    def red_dagger(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        this.DealDamage(effect.targets, 2, effect)
        Faces.ReturnToHand([this], initiator, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.Interrupt,
            "This",
            red_dagger
        ).SetCost(Cost("2", different_type=True))
        .SetTarget(Enemy),
    ]

