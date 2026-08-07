from . import *

# * Tombstone

def GetAbilities() -> Sequence['Ability']:

    def tombstone(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        Players.DiscardResourceIconFromHand(player, 1, effect, color=["B", "R"])

    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            tombstone
        ),
    ]

