from . import *

# Winds of Watoomb

def GetAbilities() -> Sequence['Ability']:

    def winds_of_watoomb(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        initiator.DrawUp(3, effect)

        PlaceThisCardInInvocationDeckDiscardPile(this, effect)

    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            winds_of_watoomb
        ).NoOutOfPlayLimit(),
    ]

