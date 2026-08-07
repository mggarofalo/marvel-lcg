from . import *

# * Rocket Raccoon

def GetAbilities() -> Sequence['Ability']:

    def rocket_raccoon(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.GainForThisActive(effect, message, attack=3)
        message.GainOverKill(effect)

    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="GUARDIAN"),
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.Interrupt,
            "This",
            Minion,
            rocket_raccoon,
        ),
    ]

