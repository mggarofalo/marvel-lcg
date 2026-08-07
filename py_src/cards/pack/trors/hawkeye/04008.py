from . import *

# Cable Arrow

def GetAbilities() -> Sequence['Ability']:

    def cable_arrow(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)
        this.RemoveThreatFromSchemes(effect.targets, 3, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            cable_arrow,
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetCostFunc(CostFunc.Exhaust(
            card_type=Upgrade,
            name=HAWKEYE_BOW_NAME,
            from_where=["YouControlCards"])
        )
        .SetIgnoreKeyword('Crisis')
    ]

