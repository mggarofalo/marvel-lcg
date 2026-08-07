from . import *

# Resize

def GetAbilities() -> Sequence['Ability']:

    def resize(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()

        hero.ChangeToOtherHeroForm(effect)

        initiator.DrawUp(1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            resize
        ).SetPlay()
    ]

