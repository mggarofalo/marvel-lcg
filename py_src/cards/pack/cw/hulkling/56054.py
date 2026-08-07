from . import *

# Complicated Lineage

def GetAbilities() -> Sequence['Ability']:

    def complicated_lineage(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            faces = player.GetControlCards2(CardFinder2("SHAPESHIFT", Upgrade))
            Faces.DiscardAll(faces, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Teddy Altman → remove Complicated Lineage from the game.",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Discard each [[Shapeshift]] upgrade you control. Discard this card.",
                action
            ),
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            complicated_lineage
        ),
    ]

