from . import *

# Grappling Hook

def GetAbilities() -> Sequence['Ability']:

    def grappling_hook(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.CancelAllEffectsAndDiscardIt(effect)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "You",
            Treachery,
            "All",
            grappling_hook
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

