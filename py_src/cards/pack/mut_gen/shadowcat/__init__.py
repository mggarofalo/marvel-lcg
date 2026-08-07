from cards.pack import *

def FlipSolidPhased(effect: 'Effect', message: 'Message.AfterUnitAttackEnd|Message.AfterUnitDefendEnd', name: 'Form.MASS_FORM_TYPE') -> None:
    this = effect.this.CastTo(Upgrade)
    Unused(this)

    initiator = effect.GetInitiator()

    # Prevent "32031a" trigger immediately
    RunAt.AfterEventEnd(
        effect,
        message,
        lambda:
            initiator.form.ChangeMassForm(effect, name=name)
    )

    # assert isinstance(end_message, Message.WhenUnitWouldAttack|Message.WhenCardResolveAbility)

