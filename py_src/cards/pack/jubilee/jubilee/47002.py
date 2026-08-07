from . import *

# * Wolverine: Logan

def GetAbilities() -> Sequence['Ability']:

    def wolverine(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.HealthUnits(effect.targets, 3, effect)


    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True,
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Response,
            "You",
            wolverine,
            to_form=AlterEgo
        ).SetTarget("This", canbe_heal=True),
    ]

