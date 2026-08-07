from . import *

# I've Got a Plan

def GetAbilities() -> Sequence['Ability']:

    def ive_got_a_plan(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        hero = effect.targets[0].CastTo(Hero)
        hero.GainUntilPhaseEnd(effect, thwart=1)


    return [
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.HeroResponse,
            "You",
            ive_got_a_plan,
            is_basic_thwart=True,
        ).SetPlay().SetLabel()
        .SetTarget("YourHero"),
    ]

