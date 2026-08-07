from . import *

# Float Like a Butterfly

def GetAbilities() -> Sequence['Ability']:

    def float_like_a_butterfly(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.IncreaseDamage(1, effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players"
        ),
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.Interrupt,
            "YouControlCharacter",
            CardFinder(is_confused=True, card_type=Enemy),
            float_like_a_butterfly,
        ),
    ]

