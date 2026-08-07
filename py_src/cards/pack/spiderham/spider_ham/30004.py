from . import *

# Hogwashed

def GetAbilities() -> Sequence['Ability']:

    def hogwashed_minion(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        LoudlyReadThisCardsFlavorText()
        this.DealDamage(effect.targets, 5, effect)

    def hogwashed_scheme(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        LoudlyReadThisCardsFlavorText()
        this.RemoveThreatFromSchemes(effect.targets, 5, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            hogwashed_minion
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Counter(Select.From("YourIdentity"), 1, 'toon'))
        .SetTarget(Minion)
        .SetName("deal 5 damage to a minion"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            hogwashed_scheme
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Counter(Select.From("YourIdentity"), 1, 'toon'))
        .SetTarget(SchemeSide2)
        .SetName("remove 5 threat from a side scheme")
    ]

