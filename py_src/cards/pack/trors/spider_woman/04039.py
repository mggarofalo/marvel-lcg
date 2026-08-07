from . import *

# Self-Propelled Glide

def GetAbilities() -> Sequence['Ability']:

    def self_propelled_glide(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        for target in effect.targets:
            target.GainUntilRoundEnd(
                effect,
                trait="AERIAL"
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            self_propelled_glide
        ).SetPlay().SetLabel()
        .SetTarget(name="Spider-Woman"),
    ]

