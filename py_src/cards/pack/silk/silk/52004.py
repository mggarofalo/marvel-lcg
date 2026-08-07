from . import *

# Wallcrawl

def GetAbilities() -> Sequence['Ability']:

    def wallcrawl(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.RemoveThreatFromSchemes(effect.targets, 2, effect)

        def action(targets: Sequence['CardFace']):
            if YouMayDiscardACardTuckedUnderSilkFromTheSameSet(initiator, targets, effect):
                this.RemoveThreatFromSchemes(targets, 3, effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Choose a scheme",
                action
            ).SetTarget(Scheme2)
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            wallcrawl
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

