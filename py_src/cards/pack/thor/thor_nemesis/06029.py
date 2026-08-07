from . import *

# Frost Giant

def GetAbilities() -> Sequence['Ability']:

    def frost_giant_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.would_atk_message.IfThisAttackDealDamage(
            lambda target:
                Faces.GiveStatus([target], "Stunned", effect),
            effect
        )


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            frost_giant_boost,
            during_attack=True,
            attacker=Villain
        ),
    ]

