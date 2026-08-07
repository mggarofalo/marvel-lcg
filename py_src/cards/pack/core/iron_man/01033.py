from . import *

# * Pepper Potts

def GetAbilities() -> Sequence['Ability']:

    def petter_potts(effect: 'Effect', message: 'Message.CheckPlayerCanPayCost') -> 'Resources|None':
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        if initiator.discard_pile.GetSize() > 0:
            face = initiator.discard_pile.Get(True)[0]
            res = FacesCounter.GetPrintedResources([face])
            return res

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            resources_fn=petter_potts,
            conditions=[
                lambda effect, message:
                    effect.this.GetControlByPlayer().discard_pile.GetSize() > 0
            ],
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

