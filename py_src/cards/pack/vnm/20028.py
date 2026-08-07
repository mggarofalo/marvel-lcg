from . import *

# Shake it Off

def GetAbilities() -> Sequence['Ability']:

    def shake_it_off(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.HeroResponse,
            None,
            shake_it_off,
            is_from_attack=True,
            conditions=[
                lambda effect, message:
                    message.trigger.HasTrait('GUARDIAN'),
            ]
        ).SetPlay().SetLabel()
        .SetTarget("Trigger", canbe_tough=True),
    ]

