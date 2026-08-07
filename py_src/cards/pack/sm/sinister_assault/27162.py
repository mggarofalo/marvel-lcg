from . import *

# Scorpion

def GetAbilities() -> Sequence['Ability']:

    def scorpion(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        target = message.attacked
        if target.IsStunned():
            this.DealDamage([target], 2, effect)
        else:
            Faces.GiveStatus([target], "Stunned", effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "Character",
            scorpion
        ),
    ]

