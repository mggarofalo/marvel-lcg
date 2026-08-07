from . import *

# Invulnerability

def GetAbilities() -> Sequence['Ability']:

    def invulnerability(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            invulnerability
        ).SetPlay().SetLabel()
        .SetTarget("YourHero", canbe_tough=True),
    ]

