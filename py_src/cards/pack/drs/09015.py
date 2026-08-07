from . import *

# Desperate Defense

def GetAbilities() -> Sequence['Ability']:

    def desperate_defense(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        message.GainDEFForThisAttack(2, effect)

        message.IfYouTakeNoDamage(
            lambda: Faces.ReadyAll([initiator.GetHero()], effect)
        )


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.HeroInterrupt,
            "YourHero",
            desperate_defense,
        ).SetPlay()
    ]

