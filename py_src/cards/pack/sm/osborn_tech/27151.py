from . import *

# Spiked Gauntlet

def GetAbilities() -> Sequence['Ability']:

    def spiked_gauntlet(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        initiator = effect.GetInitiator()

        villain = Worlds.FindVillain(effect)
        if villain:
            took_damage = False

            def action(atk_message: 'Message.WhenUnitWouldAttack'):
                def set_took():
                    nonlocal took_damage
                    took_damage = True

                this.effect.RegisterTemp(
                    AbilityFactory.AfterUnitTookDamage(
                        AbilityType.Temp0,
                        initiator.GetIdentity(),
                        lambda effect, message:
                            set_took(),
                        is_from_attack=atk_message
                    ),
                    unregister_after_exec=True,
                    until_event_end=atk_message
                )

            villain.DoAttackYou(initiator, effect, operation=action)

            if not took_damage:
                Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            spiked_gauntlet,
        ),
    ]

