from . import *

# Weapons Master

def GetAbilities() -> Sequence['Ability']:

    def weapons_master(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Nebula", card_type=Hero),
            retaliate=1,
        ),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            weapons_master
        ).SetLabel('attack').SetTarget(Enemy),
    ]

