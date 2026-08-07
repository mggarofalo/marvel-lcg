from . import *

# Paladin's Pistol

def GetAbilities() -> Sequence['Ability']:

    def paladins_pistol(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.GainRanged(effect)
        message.GainOverKill(effect)

        def action():
            Faces.DiscardAll([this], effect)
        RunAt.AfterEventEnd(effect, message, action)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Paladin"),
            otherwise_attach_to="EnemyLeader"
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            "AttachedCharacter",
            paladins_pistol
        ),
    ]

