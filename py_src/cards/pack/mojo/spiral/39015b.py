from . import *

# Across the Mojoverse

def GetAbilities() -> Sequence['Ability']:

    def across_the_mojoverse(effect: 'Effect', message: 'Message.WhenCardWouldDiscard') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        message.SetBeInstead(effect)

        message.trigger.card.MoveToBottom(GetShowDeck(effect).deck, effect)


    return [
        AbilityFactory.WhenCardWouldDiscard(
            AbilityType.ForcedInterrupt,
            CardFinder2("SHOW", Environment),
            across_the_mojoverse,
        ),
    ]

