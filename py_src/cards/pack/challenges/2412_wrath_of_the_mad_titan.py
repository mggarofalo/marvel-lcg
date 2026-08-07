from . import *

# Wrath of the Mad Titan

def GetAbilities() -> Sequence['Ability']:

    def wrath_of_the_mad_titan(effect: 'Effect', message: 'Message.WhenGameWouldBegin'):
        this = effect.this.CastTo(Challenge)
        Unused(this)
    
        scheme = Worlds.FindMainScheme(effect)
        if scheme:
            scheme.Advance("2A", effect)


    return [
        AbilityFactory.UsingOnlyHeroes(
            has_traits=["AVENGER", "GUARDIAN"]
        ),
        AbilityFactory.TheFinalBlowCanOnlyBeDealtBy(
            CardFinder(card_type=Hero, with_health=1)
        ),
        AbilityFactory.AfterGameSetup(
            wrath_of_the_mad_titan
        )
    ]

