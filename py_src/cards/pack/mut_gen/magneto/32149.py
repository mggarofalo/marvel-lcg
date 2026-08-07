from . import *

# Magnetic Bubble

def GetAbilities() -> Sequence['Ability']:

    def magnetic_bubble(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)

        Faces.PlaceCountersOn([this], message.will_take_damage, 'damage', effect)

        if this.GetCounters('damage') >= 8:
            Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Magneto")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Magneto"),
            retaliate=1,
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Magneto"),
            magnetic_bubble,
        )
    ]

