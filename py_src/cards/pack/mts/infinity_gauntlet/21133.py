from . import *

# Soul Stone

def GetAbilities() -> Sequence['Ability']:

    def soul_stone_revealed(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            this.HealthUnits([villain], 3, effect)
            Faces.GiveFacedownBoostCards([villain], 1, effect)

        PlaceThisCardInTheInfinityStoneDeckDiscardPile(effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            soul_stone_revealed
        ),
    ]

