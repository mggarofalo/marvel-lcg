from . import *

# Crimson Bands of Cyttorak

def GetAbilities() -> Sequence['Ability']:

    def crimson_bands_of_cyttorak(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)

        this.DealDamage(effect.targets, 7, effect)
        PlaceThisCardInInvocationDeckDiscardPile(this, effect)

    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            crimson_bands_of_cyttorak
        ).SetTarget(Enemy, range=(0, 1))
        .NoOutOfPlayLimit(),
    ]

