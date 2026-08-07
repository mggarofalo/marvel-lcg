from . import *

# * Scott Lang

def GetAbilities() -> Sequence['Ability']:

    def scott_lang(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)

    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Response,
            "You",
            scott_lang,
            to_this_form=True,
        ).SetTarget("YourIdentity", canbe_heal=True)
        .SetName("Time to Unwind")
    ]

