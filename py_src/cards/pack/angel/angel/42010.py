from . import *

# Techno-Organic Wings

def GetAbilities() -> Sequence['Ability']:

    def techno_organic_wings(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

    def techno_organic_wings_archangel(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        Worlds.UpdateNextCardPlayCost(
            initiator,
            -2,
            effect,
            finder=CardFinder2("AERIAL", Event),
            in_this="Phase"
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            techno_organic_wings,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetIdentity().IsName("Angel")
            ]
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("YourHero", canbe_ready=True),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            techno_organic_wings_archangel,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetIdentity().IsName("Archangel")
            ]
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

