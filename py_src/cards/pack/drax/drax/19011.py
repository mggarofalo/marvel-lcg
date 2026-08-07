from . import *

# Too Stubborn to Die

def GetAbilities() -> Sequence['Ability']:

    def too_stubborn_to_die(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.SetBeInstead(effect)
        for target in effect.targets:
            target = target.CastTo(Hero)
            target.SetHealth(4, effect)
            target.ChangeToForm(AlterEgo, effect)
            Faces.RemoveAllFromGame([this], effect)


    return [
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.HeroInterrupt,
            CardFinder(name="Drax"),
            too_stubborn_to_die,
        ).SetTarget("Trigger")
    ]

