from cards.pack.gmw.collector_1 import GetTheCollection
from . import *

# Complete the Collection

def GetAbilities() -> Sequence['Ability']:

    def complete_the_collection(effect: 'Effect', message: 'Message.WhenCardWouldMoveToArea') -> bool:
        this = effect.this.CastTo(Challenge)
        Unused(this)

        if message.by_effect.is_rule:
            return False

        deck = GetTheCollection(effect)
        return message.from_area == deck.deck

    return [
        AbilityFactory.WhenCardWouldMoveToArea(
            AbilityType.Challenge,
            None,
            lambda effect, message:
                message.SetCannot(effect),
            conditions=[complete_the_collection]
        ),
        AbilityFactory.IgnoreAbilityOnCard(
            CardFinder(name="The Grand Collection", card_type=MainScheme),
            "NonKeyword",
        )
    ]

