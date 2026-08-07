from . import *

# * Radioactive Man

def GetAbilities() -> Sequence['Ability']:

    def radioactive_man_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        player = message.GetToPlayer()

        player.DiscardRandomHandCards(1, effect)

    def radioactive_man(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        player = message.GetToPlayer()
        player.DiscardRandomHandCards(1, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            radioactive_man_boost
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            radioactive_man
        )
    ]
