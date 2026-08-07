from . import *

# Energy Truncheon

def GetAbilities() -> Sequence['Ability']:

    def energy_truncheon(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        initiator = effect.GetInitiator()

        enemy = this.GetBindFace().CastTo(Enemy)
        enemy.DoAttackYou(initiator, effect)

        Faces.DiscardAll([this], effect)
        initiator.DrawUp(1, effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Joystick"),
            otherwise_attach_to=Villain
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedEnemy",
            piercing=True
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            energy_truncheon
        ),
    ]

