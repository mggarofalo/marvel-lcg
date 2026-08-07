from . import *

# * Chamber: Jono Starsmore

def GetAbilities() -> Sequence['Ability']:

    def chamber(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.TemporaryGain(
            effect,
            message.would_atk_message,
            attack_consequential_damage=-1,
        )

    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.NonKeywordStar,
            "This",
            CardFinder(is_confused=True, card_type=Enemy),
            chamber,
        ),
    ]

