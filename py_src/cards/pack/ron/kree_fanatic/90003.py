from . import *

# The Accused

def GetAbilities() -> Sequence['Ability']:

    def the_accused(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.GainATKForThisAttack(+1, effect)

    def the_accused_discard(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.DiscardAll([this], effect)

    def the_accused_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        this.AttachTo2(identity, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        AbilityFactory.WhenUnitInitiatesAttackAgainst(
            AbilityType.ForcedInterrupt,
            Enemy,
            "AttachedPlayer",
            the_accused
        ),
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.ForcedResponse,
            CardFinder(name="Ronan the Accuser"),
            the_accused_discard
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            the_accused_boost
        ),
    ]

