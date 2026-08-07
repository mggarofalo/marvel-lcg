from . import *

# Inspiring Presence

def GetAbilities() -> Sequence['Ability']:

    def inspiring_presence(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)
        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            inspiring_presence
        ).SetPlay(only_if_your_identity_has_trait="AVENGER").SetLabel()
        .SetTarget(
            CardFinder(card_type=Ally, canbe_heal=True)|
            CardFinder(card_type=Ally, canbe_ready=True)
        ),
    ]

