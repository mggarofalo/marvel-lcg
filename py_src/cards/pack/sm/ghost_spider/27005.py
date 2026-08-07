from . import *

# Pirouette and Punch

def GetAbilities() -> Sequence['Ability']:

    def pirouette_and_punch(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        face = message.trigger
        damage = 1 + FacesCounter.CountTotalBoostIcons([face])

        this.DealDamage(effect.targets, damage, effect)

        message.CancelWhenRevealedEffect(effect)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "AnyPlayer",
            None,
            None,
            pirouette_and_punch,
            from_encounter=True,
        ).SetPlay().SetLabel()
        .SetTarget(Villain),
    ]

