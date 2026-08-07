from . import *

# Morale Boost

def GetAbilities() -> Sequence['Ability']:

    def morale_boost(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        for face in effect.targets:
            face.GainUntilRoundEnd(
                effect,
                thwart=1,
                attack=1,
                defense=1
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            morale_boost
        ).SetPlay().SetLabel()
        .SetTarget(Hero),
    ]

