from . import *

# Teamwork

def GetAbilities() -> Sequence['Ability']:

    def teamwork(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        ally = effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards[0].CastTo(Ally)

        hero = initiator.GetHero()
        if message.power == "ATK":
            value = ally.attack
            hero.GainForThisActive(effect, message.would_message, attack=value)
        if message.power == "THW":
            value = ally.thwart
            hero.GainForThisActive(effect, message.would_message, thwart=value)

    return [
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.HeroInterrupt,
            "You",
            teamwork,
            powers=["THW", "ATK"]
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Exhaust("YourAlly")),
    ]

