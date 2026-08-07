from . import *

# Mind Scan

def GetAbilities() -> Sequence['Ability']:

    def mind_scan(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = 3
        value += Worlds.VictoryDisplay(effect).FindCardSize(CardFinder(card_type=SchemeSide2))
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            mind_scan
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

