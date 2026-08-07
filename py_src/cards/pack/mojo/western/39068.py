from . import *

# Card Shark

def GetAbilities() -> Sequence['Ability']:

    def card_shark(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            faces = player.DiscardDeckTopCards(3, effect)
            num = FacesCounter.GetPrintedResourcesTypes(faces)
            identity = player.GetIdentity()
            identity.TakeIndirectDamage(this, num, effect)

    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            card_shark,
        ),
    ]

