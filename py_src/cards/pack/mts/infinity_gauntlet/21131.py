from . import *

# Power Stone

def GetAbilities() -> Sequence['Ability']:

    def power_stone_revealed(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.GetToPlayer()
        if player:
            identity = player.GetIdentity()
            if identity.IsStunned():
                identity.TakeDamage(this, 3, effect)
            else:
                Faces.GiveStatus([identity], "Stunned", effect)

        PlaceThisCardInTheInfinityStoneDeckDiscardPile(effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            power_stone_revealed
        ),
    ]

