from . import *

# Back to the Future

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.PlayersCannotRemoveThreatFrom(
            PlayerFinder(name="Cable"),
            CardFinder(non_name="Back to the Future"),
        ),
        AbilityFactory.PlayersCannotRemoveThreatFrom(
            PlayerFinder(non_name="Cable"),
            CardFinder(name="Back to the Future"),
        ),
        AbilityFactory.PlayersCannotDamageUnit(
            PlayerFinder(name="Cable"),
            Enemy,
            unit_engaged_player=PlayerFinder(non_name="Cable"),
        ),
        AbilityFactory.PlayersCannotDamageUnit(
            PlayerFinder(non_name="Cable"),
            Minion,
            unit_engaged_player=PlayerFinder(name="Cable"),
        ),
    ]

