from . import *

# * Jubilee's Coat

def GetAbilities() -> Sequence['Ability']:

    def jubilees_coat(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        value = message.paid_resources.GetResourceIconTypes()
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Jubilee"),
            thwart=1,
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.HeroResponse,
            "You",
            CardFinder2("THWART", Event),
            jubilees_coat
        ).SetLabel('thwart')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Scheme2),
    ]

