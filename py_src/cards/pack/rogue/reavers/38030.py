from . import *

# * Skullbuster

def GetAbilities() -> Sequence['Ability']:

    def skullbuster(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        value = len(player.GetEngagedMinions(CardFinder2("REAVER")))
        this.PlaceThreatOnSchemes("MainScheme", value, effect)


    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "You",
            skullbuster
        ),
    ]

