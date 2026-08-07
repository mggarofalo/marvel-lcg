from . import *

# Surprise Move

def GetAbilities() -> Sequence['Ability']:

    def surprise_move(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        message.GainAttackForThisAttack(+2, effect)

        def action(unit: Unit2):
            Faces.ReadyAll([initiator.GetIdentity()], effect)
        message.IfThisAttackDefeats(message.attacked_targets, action)


    return [
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.HeroInterrupt,
            "You",
            CardFinder(card_type=Enemy, with_attach=Upgrade),
            surprise_move,
            is_basic_attack=True,
        ).SetPlay().SetLabel(),
    ]

