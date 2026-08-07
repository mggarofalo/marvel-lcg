from . import *

# * Bonebreaker

def GetAbilities() -> Sequence['Ability']:

    def bonebreaker(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        value = len(player.GetEngagedMinions(CardFinder2("REAVER")))

        identity.TakeIndirectDamage(this, value, effect)


    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "You",
            bonebreaker
        ),
    ]

