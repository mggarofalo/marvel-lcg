from . import *

# Incendiary Bombs

def GetAbilities() -> Sequence['Ability']:

    def incendiary_bombs(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()

        player.GetIdentity().TakeIndirectDamage(this, 2, effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            Villain,
            "You",
            incendiary_bombs
        ).SetCostFunc(CostFunc.Counter("This", 1, 'bomb'))
    ]

