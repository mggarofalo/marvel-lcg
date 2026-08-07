from . import *

# * Tempo

def GetAbilities() -> Sequence['Ability']:

    def tempo(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        size = player.hand_cards.GetSize() * 2
        player.DiscardDeckTopCards(size, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "EngagedIdentity",
            hand_size=1,
        ),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            tempo,
        ),
    ]

