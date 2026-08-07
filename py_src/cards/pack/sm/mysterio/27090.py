from . import *

# Masterful Mirage

def GetAbilities() -> Sequence['Ability']:

    def masterful_mirage(effect: 'Effect', message: 'Message.WhenFaceWouldDealDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)
        player = message.source.GetControlByPlayer()
        player.DiscardDeckTopCards(4, effect)
        Faces.DiscardAll([this], effect)


    def masterful_mirage_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.GiveBoostCardForThisActivation(Villain, 1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name=MYSTERIO)
        ),
        AbilityFactory.WhenFaceWouldDealDamage(
            AbilityType.ForcedInterrupt,
            "You",
            masterful_mirage,
            who_take_damage=CardFinder(name=MYSTERIO),
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            masterful_mirage_boost
        ),
    ]

