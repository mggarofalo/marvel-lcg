from . import *

# Genosha

def GetAbilities() -> Sequence['Ability']:

    def genosha(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        this.PlaceThreatOnSchemes("MainScheme", 1, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Villain,
            steady=1,
        ),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            genosha
        ),
        WhenRevealedDiscardOtherSettingEnviroment()
    ]

