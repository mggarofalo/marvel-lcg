from . import *

# * Daredevil

def GetAbilities() -> Sequence['Ability']:

    def daredevil(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.MoveDamage(1, message.attacker, effect)


    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.Response,
            "This",
            daredevil,
            conditions=[
                lambda effect, message:
                    effect.this.CastTo(Unit2).sustained > 0 and \
                    Enemy.IsType(message.attacker),
            ]
        ),
    ]

