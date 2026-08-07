from . import *

# Maximum Velocity

def GetAbilities() -> Sequence['Ability']:

    def maximum_velocity(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        hero.GainUntilRoundEnd(effect, thwart=2, attack=2, defense=2)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            maximum_velocity
        ).SetPlay()
        .MaxOnePerPhase()
    ]

