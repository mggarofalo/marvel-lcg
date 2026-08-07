from . import *

# Height Advantage

def GetAbilities() -> Sequence['Ability']:

    def height_advantage(effect: 'Effect', message: 'Message.WhenPlayerTurnBegin') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.NonKeyword,
            "You",
            lambda effect, message:
                message.ReduceDamage(1, effect),
            is_from_attack=True,
            who_deal_damage=Enemy,
        ),
        AbilityFactory.WhenPlayerTurnBegin(
            AbilityType.ForcedInterrupt,
            "You",
            height_advantage
        ),
    ]

