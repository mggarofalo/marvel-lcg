from . import *

# Energy Duplication

def GetAbilities() -> Sequence['Ability']:

    def energy_duplication(effect: 'Effect', message: 'Message.CheckPlayerCanPayCost') -> 'Resources|None':
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.form.FindEnergyForm()
        if face:
            return FacesCounter.GetPrintedResources([face])


    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.HeroResource,
            resources_fn=energy_duplication
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

