from . import *

# Home by Dawn

def GetAbilities() -> Sequence['Ability']:

    def home_by_dawn(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def discard_cards(targets: Sequence['CardFace']):
            Faces.DiscardAll(targets, effect)
            Faces.DiscardAll([this], effect)

        def gain_surge(targets: Sequence['CardFace']):
            ThisCardGainSurge(effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Kamala Khan → remove Home by Dawn from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
            ).SetCostFunc(CostFunc.Exhaust("YourAlterEgo")),
            AbilityFactory.ForChoiceAbility(
                "Discard 1 [[Persona]] support you control",
                discard_cards
            ).SetTarget(Support, trait="PERSONA", from_where=["YouControlCards"], canbe_discard=True),
            AbilityFactory.Otherwise(
                gain_surge
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            home_by_dawn
        )
    ]

