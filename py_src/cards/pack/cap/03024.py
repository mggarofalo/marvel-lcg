from . import *

# * Avengers Tower

def GetAbilities() -> Sequence['Ability']:

    def avengers_tower(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Worlds.UpdateNextCardPlayCost(
            "Any",
            -1,
            effect,
            finder=CardFinder2("AVENGER", Ally),
            in_this="Phase"
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            avengers_tower
        ).SetCostFunc(CostFunc.Exhaust("This")),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "You",
            if_each_of_your_allies_has_trait="AVENGER",
            ally_limit=1,
        )
    ]

