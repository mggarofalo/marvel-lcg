from . import *

# In Defiance

def GetAbilities() -> Sequence['Ability']:

    def in_defiance(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        message.PreventDamage(2, effect)
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            Identity,
            in_defiance,
            is_from_attack=True,
        ).SetPlay().SetLabel()
        .SetHasNoTargetEffect(),
    ]

