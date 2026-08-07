from . import *

# * Utopia

def GetAbilities() -> Sequence['Ability']:

    def utopia(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "You",
            if_each_of_your_allies_has_trait="X-MEN",
            ally_limit=1,
        ),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            CardFinder2("X-MEN", Ally),
            utopia,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Unit2, trait="X-MEN", canbe_ready=True),
    ]

