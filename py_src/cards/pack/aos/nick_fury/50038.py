from . import *

# Covert Surveillance

def GetAbilities() -> Sequence['Ability']:

    def covert_surveillance(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = this.RemoveThreatFromSchemes(effect.targets, 2, effect)
        if initiator.form.IsInSuitForm("Stealth"):
            if value:
                initiator.MayChooseOneAbility(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "Place that threat on your suit form upgrade",
                        lambda targets:
                            Faces.PlaceTokensOn(targets, value, 'threat', effect)
                    ).SetTarget(Upgrade, form="Suit", from_where=["YouControlCards"])
                )
        else:
            initiator.MayChooseOneAbility(
                effect,
                ChangeToStealthSuitForm(initiator, effect)
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            covert_surveillance
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

