from . import *

# Ultron Drones

def GetAbilities() -> Sequence['Ability']:
    # There is deliberately no AfterUnitBeDefeated ability here. Unit2's
    # generic defeat cleanup resets a facedown drone to its printed card before
    # discarding it, then routes player cards to their owner's discard pile and
    # encounter cards to the encounter discard pile. Registering the printed
    # Forced Response as an empty handler would only pretend to implement work
    # the defeat rule has already completed.
    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Drone Minion", card_type=Minion),
            base_sch=1,
            base_atk=1,
            base_health=1,
        ),
    ]
