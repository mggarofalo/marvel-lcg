from . import *

# Hurricane

def GetAbilities() -> Sequence['Ability']:

    def hurricane(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            hurricane
        ).SetTarget(Scheme2),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "Character",
            retaliate=1,
        )
    ]

