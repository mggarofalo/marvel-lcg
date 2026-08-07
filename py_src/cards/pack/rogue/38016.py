from . import *

# Not Today!

def GetAbilities() -> Sequence['Ability']:

    def not_today(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.GainDEFForThisAttack(+2, effect)
        
        def action():
            initiator = effect.GetInitiator()
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, 2, effect)
                ).SetTarget(Scheme2)
            )

        message.IfYouTakeNoDamage(action)


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.HeroInterrupt,
            "YourHero",
            not_today
        ).SetPlay().SetLabel('defense'),
    ]

