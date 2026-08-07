from . import *

# * Headpool: Wade "Shorty" Wilson

def GetAbilities() -> Sequence['Ability']:

    def headpool(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        minion = message.attacked.CastTo(Minion)
        for target in effect.targets:
            enemy = target.CastTo(Enemy)
            minion.BasicAttack([enemy], effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.Response,
            "This",
            Minion,
            headpool,
            target_in_play=True,
        ).SetTarget(Enemy, check_fn=lambda effect, face: face != effect.GetBindMessage(Message.AfterUnitAttackUnit).attacked)
    ]

