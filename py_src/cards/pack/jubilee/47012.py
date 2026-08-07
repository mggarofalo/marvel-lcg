from . import *

# * Husk: Paige Guthrie

def GetAbilities() -> Sequence['Ability']:

    def husk(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        res = effect.GetPaidResources(True)
        if res.HasColor("Y"):
            message.GainValue(+1, effect)
        if res.HasColor("B"):
            this.HealthUnits([this], 1, effect)
        if res.HasColor("R"):
            RunAt.AfterPowerUsed(effect, message, lambda: Faces.ReadyAll([this], effect))


    return [
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.Interrupt,
            "This",
            husk
        ).SetCost(Cost("3", up_to=True)),
    ]

