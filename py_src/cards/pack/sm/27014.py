from . import *

# Jump Flip

def GetAbilities() -> Sequence['Ability']:

    def jump_flip(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        message.PreventDamage(2, effect)
        
        if effect.GetPaidResources().HasColor("Y"):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, 2, effect)
                ).SetTarget(MainScheme)
            )

    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            "You",
            jump_flip
        ).SetPlay().SetLabel('defense'),
    ]

