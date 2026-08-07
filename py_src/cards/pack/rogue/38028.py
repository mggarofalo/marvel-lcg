from . import *

# Med Lab

def GetAbilities() -> Sequence['Ability']:

    def med_lab_response(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.PlaceCardHere(effect.targets, True, effect)

    def med_lab(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        def action(effect: 'Effect', message: 'Message.WhenCardEnterPlay'):
            Faces.ExhaustAll([message.trigger], effect)

        this.effect.RegisterTemp(
            AbilityFactory.WhenCardEnterPlay(
                AbilityType.Temp0,
                effect.targets,
                action,
            ),
            unregister_after_exec=True,
            until_phase_end=True
        )

        initiator = effect.GetInitiator()
        initiator.PlayCardsLikeInTurn(effect.targets, effect)

    return [
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.Response,
            Ally,
            med_lab_response,
            by_consequential=True,
            conditions=[
                lambda effect, message:
                    effect.this.GetPlacedCardArea().GetSize() == 0
            ]
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Trigger"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            med_lab
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Ally, from_where=["ThisPlaceCards"]),
    ]

