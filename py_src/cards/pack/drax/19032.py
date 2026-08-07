from . import *

# Regroup

def GetAbilities() -> Sequence['Ability']:

    def regroup(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.ReturnToHand(effect.targets, "Owner", effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.Interrupt,
            Ally,
            regroup,
            by_attack=True,
            attacker=Enemy,
            conditions=[
                lambda effect, message:
                    message.trigger.IsInPlay()
            ],
        ).SetTarget("Trigger"),
        AbilityFactory.DiscardThisWhenRoundEnd(
            AbilityType.ForcedInterrupt,
        )
    ]

