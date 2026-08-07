from . import *

# * Bishop's Uniform

def GetAbilities() -> Sequence['Ability']:

    def bishops_uniform(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        size = len(initiator.hand_cards.FindCards(card_type=Resource))
        this.HealthUnits(effect.targets, size, effect)


    return [
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.Response,
            "You",
            CardFinder(name="Bishop"),
            bishops_uniform,
            ability_name="Energy Absorption"
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Bishop"),
    ]

