from . import *

# Electro

def GetAbilities() -> Sequence['Ability']:

    def electro(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer|Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        if isinstance(message, Message.AfterMinionEngagePlayer):
            player = message.engaged_player
        else:
            player = message.GetAgainstPlayer()

        player.DiscardDeckUntil(
            effect,
            has_printed_res=["Y", "G"],
        )

    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "You",
            electro
        ),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            electro,
        ),
    ]

