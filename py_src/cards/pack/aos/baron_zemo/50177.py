from . import *

# The Ends Justify the Means

def GetAbilities() -> Sequence['Ability']:

    def the_ends_justify_the_means_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action_a(targets: Sequence['CardFace']):
            enemy = Worlds.FindCardOnField(
                effect,
                card_type=Enemy,
                name="Baron Zemo"
            )
            Faces.DefeatUnits(targets, enemy, effect)
            if enemy:
                enemy.DoSchemes(player, effect)

        def action_b(targets: Sequence['CardFace'], discard_effect: 'Effect'):
            faces = discard_effect.cost_func.Get(CostFunc.Discard).return_discarded_cards
            value = FacesCounter.GetPrintedResourceCost(faces)
            RemoveSecretCountersFromAmongBoardMemberEnvironments(value, effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Baron Zemo defeats the minion with the fewest remaining hit points. Baron Zemo schemes.",
                action_a,
            ).SetTarget(Minion, fewest_remaining_hp=True)
            .SetHasNoTargetEffect(),
            AbilityFactory.ForChoiceAbility3(
                "Discard an ally or support you control → remove secret counters from among [[Board Member]] environments equal to the discarded card's printed resource cost.",
                action_b
            ).SetCostFunc(CostFunc.Discard("YouControlCards", card_type=Ally|Support))
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_ends_justify_the_means_revealed
        ),
    ]

