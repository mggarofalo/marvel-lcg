from . import *

# Mark V Armor

def GetAbilities() -> Sequence['Ability']:

    def mark_v_armor(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)
        Faces.PlaceCountersOn([this], message.will_take_damage, 'damage', effect)

        if this.GetCounters('damage') >= 5:
            Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Iron Man"),
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Iron Man"),
            mark_v_armor

        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

