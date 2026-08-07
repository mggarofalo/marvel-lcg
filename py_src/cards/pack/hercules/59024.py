from . import *

# "Avenge Me!"

def GetAbilities() -> Sequence['Ability']:

    def avenge_me(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(2, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            Ally
        ).SetPlay(only_if_your_identity_has_trait="AVENGER"),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.Interrupt,
            "AttachedAlly",
            avenge_me
        ),
    ]

