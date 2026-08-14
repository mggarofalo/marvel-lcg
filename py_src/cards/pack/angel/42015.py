from . import *

# Ever Vigilant

def GetAbilities() -> Sequence['Ability']:

    def ever_vigilant(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        this.RemoveThreatFromSchemes(effect.targets2, 2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            ever_vigilant
        ).SetPlay(only_if_your_identity_has_trait="AERIAL")
        .SetTarget("YourHero", canbe_ready=True)
        .SetTarget2(MainScheme)
    ]

