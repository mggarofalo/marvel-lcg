from . import *

# Unyielding Persistence

def GetAbilities() -> Sequence['Ability']:

    def unyielding_persistence(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Nebula", card_type=Hero),
            thwart=1,
            attack=1,
            stalwart=1,
        ),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            unyielding_persistence
        ).SetTarget(name="Nebula", canbe_tough=True),
    ]

