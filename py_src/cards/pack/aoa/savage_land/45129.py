from . import *

# Velociraptor

def GetAbilities() -> Sequence['Ability']:

    def velociraptor(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        face = player.DiscardDeckTopCard(effect)
        value = FacesCounter.GetPrintedResourcesIcon([face], player.player_deck)
        message.GainATKForThisAttack(
            value,
            effect
        )

    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "This",
            velociraptor
        ),
    ]

