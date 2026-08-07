from . import *

# * Shatterstar: Gaveedra Seven

def GetAbilities() -> Sequence['Ability']:

    def shatterstar(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        minion = message.target
        if not Minion.IsType(minion):
            return

        initiator = effect.GetInitiator()
        assert Minion.IsType(message.target)
        if message.target.GetEngagedPlayer() == initiator:
            message.GainAttackForThisAttack(1, effect)
        else:
            minion.PutIntoPlay(initiator, effect)

    return [
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.NonKeyword,
            "This",
            Minion,
            shatterstar,
        ),
    ]

