from . import *

# Hive Mind

def GetAbilities() -> Sequence['Ability']:

    def hive_mind(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = 2

        initiator = effect.GetInitiator()
        value += len(initiator.supports.FindCards(name="Army of Ants", card_type=Support))
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            hive_mind,
            conditions=[
                lambda effect, message:
                    Condition.YouAreInHeroFrom(effect, "TINY"),
            ]
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
    ]

