from cards.pack import *

def ChangeToStealthSuitForm(initiator: Player, effect: 'Effect') -> 'Ability':

    return AbilityFactory.ForChoiceAbility(
        "Change to Stealth suit form",
        lambda targets:
            initiator.form.ChangeSuitForm(effect, "Stealth"),
        condition=not initiator.form.IsInSuitForm("Stealth")
    )

