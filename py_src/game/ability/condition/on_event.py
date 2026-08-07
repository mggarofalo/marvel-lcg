from typing import TypeAlias
from core import *
from game.ability import *
from game.message import *

class ConditionOnEventType:

    ON_EVENT: TypeAlias = 'OnEvent.Base'
    EVENT_TYPE: TypeAlias = List[ON_EVENT]|ON_EVENT

    @staticmethod
    def AddEvent(change_on_event: 'EVENT_TYPE', one_event: ON_EVENT) -> List[ON_EVENT]:
        from game.message import OnEvent
        if change_on_event == OnEvent.NONE:
            return [one_event]
        elif isinstance(change_on_event, list):
            return change_on_event + [one_event]
        else:
            return [change_on_event, one_event]

    @staticmethod
    def GetEventAbilities(change_on_event: 'EVENT_TYPE',
                        ability_type: AbilityType,
                        condition: Callable[['Effect', 'Message2'], bool],
                        operation: Callable[['Effect', 'Message2'], None]
                        ) -> List['Ability']:
        change_on_events: List['OnEvent.Base'] = []

        if isinstance(change_on_event, list):
            change_on_events = change_on_event
        else:
            change_on_events = [change_on_event]

        abilities: List['Ability'] = []
        for on_event in change_on_events:
            abilities += on_event.GenerateAbilities2(
                ability_type,
                condition,
                operation
            )

        return abilities

