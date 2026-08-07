from . import *

# Super Spies

def GetAbilities() -> Sequence['Ability']:

    def super_spies(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        targets = effect.targets2
        faces = Filter.ByType(targets, Support)
        Faces.PlaceCountersOn(faces, 1, 'all-purpose', effect)
        faces = Filter.ByType(targets, Upgrade)
        Faces.PlaceTokensOn(faces, 1, 'threat', effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            super_spies
        ).SetPlay().SetLabel()
        .SetTarget("TeamUp")
        .SetTarget2(
            CardFinder2("S.H.I.E.L.D", Support)|
            CardFinder(form="Suit", card_type=Upgrade),
            repeat_rules="Any", range=(1, 3),
        )
    ]

