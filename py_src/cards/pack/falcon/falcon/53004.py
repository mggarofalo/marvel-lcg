from . import *

# Bird's-Eye View

def GetAbilities() -> Sequence['Ability']:

    def birds_eye_view(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        value = 3

        def action(targets: Sequence['CardFace']):
            nonlocal value
            Faces.DiscardAll(targets, effect)
            value += FacesCounter.CountTotalBoostStarAndBoost(targets)

        initiator.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard the top card of the encounter deck to remove additional threat",
                action
            ).SetTarget("EncounterDeckTop")
        )

        this.RemoveThreatFromSchemes(effect.targets, value, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            birds_eye_view
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

