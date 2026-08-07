from . import *

# Expert Defense

def GetAbilities() -> Sequence['Ability']:

    def expert_defense(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.GainDEFForThisAttack(3, effect)


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.HeroInterrupt,
            "YourHero",
            expert_defense,
        ).SetPlay().SetLabel('defense'),
    ]

