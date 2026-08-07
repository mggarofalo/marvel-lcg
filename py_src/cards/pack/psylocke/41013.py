from . import *

# * Cypher: Doug Ramsey

def GetAbilities() -> Sequence['Ability']:

    def cypher(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)

    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.Response,
            "This",
            CardFinder(is_confused=True, card_type=Enemy),
            cypher,
        ),
    ]

