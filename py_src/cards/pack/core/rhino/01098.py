from . import *

# Armored Rhino Suit

def GetAbilities() -> Sequence['Ability']:

    def armored_rhino_suit(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        damage = message.PreventDamage("All", effect)
        Faces.PlaceCountersOn([this], damage, 'damage', effect)
        if this.GetCounters('damage') >= 5:
            Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Rhino")
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Rhino"),
            armored_rhino_suit
        )
    ]

