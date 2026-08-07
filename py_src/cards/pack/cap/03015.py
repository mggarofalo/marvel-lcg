from . import *

# Avengers Assemble!

def GetAbilities() -> Sequence['Ability']:

    def avengers_assemble(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

        Players.EachCharacterGetsUntilPhaseEnd(
            None,
            effect,
            CardFinder2("AVENGER", Unit2),
            thwart=+1,
            attack=+1
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            avengers_assemble
        ).SetPlay()
        .SetTarget("YouControlUnit", trait="AVENGER", range="All", canbe_ready=True)
        .SetTarget2("This")
        .MaxOnePerRound()
    ]

