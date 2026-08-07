from . import *

# Magnetic Bubble

def GetAbilities() -> Sequence['Ability']:

    def magnetic_bubble(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.SetBeInstead(effect)

        damage = message.will_take_damage
        Faces.PlaceCountersOn([this], damage, 'damage', effect)

        if this.GetCounters('damage') >= 6:
            Faces.DiscardAll([this], effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Magneto"),
            retaliate=1,
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            "You",
            magnetic_bubble
        ),
    ]

