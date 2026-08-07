from . import *

# * Captain America's Helmet

def GetAbilities() -> Sequence['Ability']:

    def captain_americas_helmet(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)

        hero = this.GetBindFace().CastTo(Unit2)
        message.SetBeInstead(effect)

        hero.SetHealth(1, effect)
        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.Interrupt,
            CardFinder(name="Captain America"),
            captain_americas_helmet
        )
    ]
