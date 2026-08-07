from . import *

# Psychic Rapport

def GetAbilities() -> Sequence['Ability']:

    def psychic_rapport(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ReadyAll(effect.targets, effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Return a Cyclops card from your discard pile to your hand",
                lambda targets:
                    Faces.ReturnToHand(targets, initiator, effect)
            ).SetTarget(deck_building_class="Cyclops", from_where=["YourDiscardPile"]),
            AbilityFactory.ForChoiceAbility(
                "Place 2 power counters on Phoenix Force",
                lambda targets:
                    Faces.PlaceCountersOn(targets, 2, 'power', effect)
            ).SetTarget(name="Phoenix Force")
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            psychic_rapport
        ).SetPlay().SetLabel()
        .SetTarget("TeamUp"),
    ]

