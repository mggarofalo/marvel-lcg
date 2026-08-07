from . import *

# Not Easy Being Green

def GetAbilities() -> Sequence['Ability']:

    return [
        # AbilityFactory.UsingOnlyHeroes(
        #     [She-Hulk, Hulk, Gamora, and/or Drax]
        #     Protection aspect
        # ),
        AbilityFactory.AfterCounterPlacedOn(
            AbilityType.Challenge,
            CardFinder(name="Criminal Enterprise"),
            'infamy',
            lambda effect, message:
                Worlds.SetGameOver(False, effect),
            at_least="6*"
        ),
    ]

