from . import *

# Onrush

def GetAbilities() -> Sequence['Ability']:

    def onrush(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.CancelAllEffectsAndDiscardIt(effect)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "AnyPlayer",
            None,
            "All",
            onrush
        ).SetPlay().SetLabel(),
    ]

