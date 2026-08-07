from cards.pack import *

def AfterGhostSpiderUsesBasicPower(ability_type: 'AbilityType',
                                   operation: Callable[['Effect', 'Message.AfterUnitUseBasicPower'], Any]):
    return AbilityFactory.AfterUnitUseBasicPower(
        ability_type,
        None,
        operation,
        conditions=[
            lambda effect, message:
                message.trigger.IsName('Ghost-Spider'),
        ]
    )

def FindGeorgeStacy(effect: 'Effect') -> Support|None:
    return Worlds.FindCardOnField(
        effect,
        name="George Stacy",
        card_type=Support
    )

