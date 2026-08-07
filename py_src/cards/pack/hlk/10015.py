from . import *

# Toe to Toe

def GetAbilities() -> Sequence['Ability']:

    def toe_to_toe(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        for target in effect.targets:
            target = target.CastTo(Minion|Villain)
            target.DoAttackYou(initiator, effect)
            this.DealDamage([target], 5, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            toe_to_toe
        ).SetPlay().SetLabel('attack').SetTarget(Enemy)
    ]
