from . import *

# Preemptive Strike

def GetAbilities() -> Sequence['Ability']:

    def preemptive_strike(effect: 'Effect', message: 'Message.WhenBoostCardTurnedFaceUp') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        # "for each boost icon **cancelled this way**" -- the return value of
        # `CancelAllBoostIcons` is what this copy actually removed, which is 0
        # once another effect has already cancelled them.
        # `message.original_boost_icons` is the snapshot taken when the boost
        # card was turned face up and keeps paying out after the icons are gone.
        damage = message.CancelAllBoostIcons(effect)

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

