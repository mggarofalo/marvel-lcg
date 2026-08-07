from . import *

# Staggering Strength

def GetAbilities() -> Sequence['Ability']:

    def staggering_strength(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetAgainstPlayer()
        Faces.GiveStatus([player.GetIdentity()], "Stunned", effect)

        def action():
            Faces.DiscardAll([this], effect)

        RunAt.AfterEnemyActivationEnd(
            effect,
            message,
            action
        )

    def staggering_strength_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            this.Reveal(player, effect)

        message.AfterThisActivation(
            effect,
            action
        )


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Apocalypse"),
            when_attach_operation=lambda face, effect:
                face.CastTo(Villain).ChangeToForm("GIANT", effect)
        ),
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Apocalypse"),
            staggering_strength
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            staggering_strength_boost
        ),
    ]

