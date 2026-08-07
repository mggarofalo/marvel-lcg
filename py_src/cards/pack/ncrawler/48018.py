from . import *

# Riposte

def GetAbilities() -> Sequence['Ability']:

    def riposte(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.GainDEFForThisAttack(+2, effect)

        def action():
            if message.attacker:
                this.DealDamage([message.attacker], 3, effect)
        message.IfYouTakeNoDamage(action)


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.HeroInterrupt,
            "YourHero",
            riposte
        ).SetPlay().SetLabel('defense'),
    ]

