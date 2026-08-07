from . import *

# Preemptive Strike

def GetAbilities() -> Sequence['Ability']:

    def preemptive_strike(effect: 'Effect', message: 'Message.WhenBoostCardTurnedFaceUp') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        damage = message.original_boost_icons
        message.CancelAllBoostIcons(effect)

        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenBoostCardTurnedFaceUp(
            AbilityType.HeroInterrupt,
            "BoostIcons",
            preemptive_strike,
            while_attack=True,
            boost_for_card=Villain,
        ).SetPlay().SetLabel('defense')
        .SetTarget("BoostForCard"),
    ]

