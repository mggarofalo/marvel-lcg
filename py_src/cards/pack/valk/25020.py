from . import *

# The Best Defense

def GetAbilities() -> Sequence['Ability']:

    def the_best_defense(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        value = hero.attack - hero.defense
        message.GainDEFForThisAttack(value, effect)


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.HeroInterrupt,
            "YourHero",
            the_best_defense,
        ).SetPlay().SetLabel('defense'),
    ]

