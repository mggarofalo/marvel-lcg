from . import *

# Inferiority Complex

def GetAbilities() -> Sequence['Ability']:

    def inferiority_complex(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            Faces.DiscardAll(targets, effect)
            if not targets:
                ThisCardGainSurge(effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust your alter-ego → remove Inferiority Complex from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Choose and discard 2 [[Technique]] upgrades you control. If no upgrade was discarded this way, this card gains surge. Discard this obligation",
                action
            ).SetTarget(Upgrade, trait="TECHNIQUE", from_where=["YouControlCards"], range=(2, 2), canbe_discard=True),
            AbilityFactory.Otherwise(
                action
            ).SetTarget(Upgrade, trait="TECHNIQUE", from_where=["YouControlCards"], range=("Zero", "All"), canbe_discard=True)
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            inferiority_complex
        ),
    ]

