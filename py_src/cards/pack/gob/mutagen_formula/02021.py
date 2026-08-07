from . import *

# Pumpkin Bombs

def GetAbilities() -> Sequence['Ability']:

    def pumpkin_bombs(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.DiscardAll([this], effect)
        message.attacked.TakeIndirectDamage(this, 2, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedInterrupt,
            Villain,
            "You",
            pumpkin_bombs
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("RR")),
    ]

