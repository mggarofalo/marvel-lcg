from . import *

# Clear Skies

def GetAbilities() -> Sequence['Ability']:

    def clear_skies(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            clear_skies
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "Character",
            stalwart=1,
        )
    ]

