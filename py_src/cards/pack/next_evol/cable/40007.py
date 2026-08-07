from . import *

# * Graymalkin

def GetAbilities() -> Sequence['Ability']:

    def graymalkin(effect: 'Effect', message: 'Message.AfterSchemeBeDefeated') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

    return [
        AbilityFactory.AfterSchemeBeDefeated(
            AbilityType.Response,
            SchemeSide2,
            graymalkin
        ).SetTarget("This", canbe_ready=True),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("Y")
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

