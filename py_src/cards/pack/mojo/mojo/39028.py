from . import *

# Stinger Tail

def GetAbilities() -> Sequence['Ability']:

    def stinger_tail(effect: 'Effect', message: 'Message.WhenFaceWouldDealDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)
        num = message.damage
        Faces.PlaceCountersOn([this], num, 'damage', effect)

        if this.GetCounters('damage') >= 5:
            Faces.DiscardAll([this], effect)

    def stinger_tail_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        villain = Worlds.FindVillain(effect, name="Mojo")
        if villain:
            this.AttachTo2(villain, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Mojo")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Mojo"),
            retaliate=2,
        ),
        AbilityFactory.WhenDamageWouldBeDealtTo(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Mojo"),
            stinger_tail,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            stinger_tail_boost
        ),
    ]

