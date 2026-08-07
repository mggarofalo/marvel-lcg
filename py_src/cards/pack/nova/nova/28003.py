from . import *

# Forcefield Projection

def GetAbilities() -> Sequence['Ability']:

    def forcefield_projection(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        message.PreventDamage(3, effect)

        if effect.GetPaidResources().HasColor("G"):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.DealDamage(targets, 3, effect)
                ).SetTarget(Enemy)
            )


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            Friend,
            forcefield_projection,
            is_from_attack=True
        ).SetPlay().SetLabel(),
    ]

