from . import *

# Super-Soldier Serum

def GetAbilities() -> Sequence['Ability']:

    def super_soldier_serum(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.GainOverKill(effect)

        def action():
            Faces.DiscardAll([this], effect)
        RunAt.AfterEventEnd(effect, message, action)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Captain America")
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Captain America"),
            super_soldier_serum
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

