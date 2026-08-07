from . import *

# Press the Advantage

def GetAbilities() -> Sequence['Ability']:

    def press_the_advantage(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.DealDamage(effect.targets, 2, effect)
        for target in effect.targets:
            # The second sentence of the ability will only apply if the enemy is stunned/confused and still in play. If it’s defeated, that sentence of the ability cannot resolve.
            if not target.IsDefeated():
                unit = target.CastTo(Unit2)
                if unit.IsConfused() or unit.IsStunned():
                    initiator.DrawUp(1, effect)
                    break


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            press_the_advantage
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

