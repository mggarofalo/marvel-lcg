from . import *

# Hysteria

def GetAbilities() -> Sequence['Ability']:

    def hysteria(effect: 'Effect', message: 'Message.WhenUnitWouldAttack|Message.WhenUnitWouldScheme') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.GiveAdditionalBoostCardForThisActivation(1, effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Green Goblin")
        ),
        AbilityFactory.WhenUnitWouldScheme(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Green Goblin"),
            hysteria
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Green Goblin"),
            hysteria
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("BB")),
    ]

