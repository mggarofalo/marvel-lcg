from . import *

# * Cyclops: Scott Summers

def GetAbilities() -> Sequence['Ability']:

    def cyclops(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.effect.RegisterTemp(
            AbilityFactory.IncreaseAllDamageUnitTakeBy(
                AbilityType.Temp0,
                list(effect.targets),
                1,
                is_from_attack=True
            ),
            unregister_after_exec=False,
            until_phase_end=True
        )


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            cyclops
        ).SetTarget(Enemy),
    ]

