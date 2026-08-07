from . import *

# Psychic Link

def GetAbilities() -> Sequence['Ability']:

    def psychic_link(effect: 'Effect', message: 'Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.GainTHWForThisThwart(2, effect)

    return [
        AbilityFactory.WhenUnitMakeThwart(
            AbilityType.HeroInterrupt,
            CardFinder(name="SP//dr Suit"),
            None,
            psychic_link,
            is_basic_thwart=True
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

