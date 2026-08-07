from . import *

# Seven Rings of Raggadorr

def GetAbilities() -> Sequence['Ability']:

    def seven_rings_of_raggadorr(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)

        PlaceThisCardInInvocationDeckDiscardPile(this, effect)

    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            seven_rings_of_raggadorr
        ).SetTarget(Unit2, canbe_tough=True, range=(0, 3))
        .NoOutOfPlayLimit()
    ]

