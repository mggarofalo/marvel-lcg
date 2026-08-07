from . import *

# * Pyro A

def GetAbilities() -> Sequence['Ability']:

    def pyro(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Villain|Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            faces = player.DiscardDeckTopCards(2, effect)
            damage = FacesCounter.GetPrintedResourcesIcon(faces, player.player_deck)
            identity = player.GetIdentity()
            identity.TakeIndirectDamage(this, damage, effect)


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            pyro
        ),
    ]

