from . import *

# * Storm's Crown

def GetAbilities() -> Sequence['Ability']:

    def storms_crown(effect: 'Effect', message: 'Message.CheckPlayerCanPayCost') -> 'Resources|None':
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        face = FindYourWeatherSupport(initiator)
        if face:
            res = FacesCounter.GetPrintedResources([face])
            return res


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Storm"),
            thwart=1,
        ),
        AbilityFactory.CanGenerateResources(
            AbilityType.HeroResource,
            resources_fn=storms_crown
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

