from . import *

# "You Got This!"

def GetAbilities() -> Sequence['Ability']:

    def you_got_this(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        allies = effect.cost_func.Get(CostFunc.Discard).return_discarded_cards
        message.AddThatMatchingPower(allies, effect)

        Faces.ReadyAll([message.trigger], effect)


    return [
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.HeroResponse,
            "YourHero",
            you_got_this,
            powers=["THW", "ATK"]
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Discard("YourAlly")),
    ]

