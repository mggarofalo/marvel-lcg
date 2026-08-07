from . import *

# Ultron Drones

def GetAbilities() -> Sequence['Ability']:

    def ultron_drones(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> None:
        pass

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Drone Minion", card_type=Minion),
            base_sch=1,
            base_atk=1,
            base_health=1,
        ),
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.ForcedResponse,
            CardFinder(name="Drone Minion"),
            ultron_drones,
        )
    ]
