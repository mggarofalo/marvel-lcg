from . import *

# * Crossbones' Armor

def GetAbilities() -> Sequence['Ability']:

    def crossbones_armor(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)
        damage = message.will_take_damage
        Faces.PlaceCountersOn([this], damage, 'damage', effect)
        if this.GetCounters('damage') >= 5:
            Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Crossbones")
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Crossbones"),
            crossbones_armor
        ),
    ]

