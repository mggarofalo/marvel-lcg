from . import *

# Pyro's Flamethrower

def GetAbilities() -> Sequence['Ability']:

    def pyros_flamethrower(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetAgainstPlayer()

        face = player.DiscardDeckTopCard(effect)
        damage = FacesCounter.GetPrintedResourcesIcon([face], player.player_deck)
        message.GainATKForThisAttack(
            damage,
            effect
        )

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Pyro"),
            if_cannot_gain_surge=True
        ),
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Pyro"),
            pyros_flamethrower
        ),
    ]

