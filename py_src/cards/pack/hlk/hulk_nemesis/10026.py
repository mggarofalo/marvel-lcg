from . import *

# * Abomination

def GetAbilities() -> Sequence['Ability']:

    def abomination(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        face = player.DiscardDeckTopCard(effect)
        res = FacesCounter.GetPrintedResources([face])
        if res.r:
            player.GetIdentity().TakeDamage(this, 2, effect)


    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            abomination
        )
    ]

