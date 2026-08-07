from . import *

# Midtown Manhattan - C

def GetAbilities() -> Sequence['Ability']:

    def midtown_manhattan(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = message.GetToPlayer()

        if player:
            damage = 2
            symbiote = Worlds.FindCardOnField(
                effect,
                trait="SYMBIOTE",
                card_type=Environment
            )
            if symbiote:
                damage += 1

            player.GetIdentity().TakeIndirectDamage(this, damage, effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            midtown_manhattan
        )
    ]

