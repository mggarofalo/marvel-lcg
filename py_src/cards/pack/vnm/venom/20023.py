from . import *

# Struggle for Control

def GetAbilities() -> Sequence['Ability']:

    def struggle_for_control(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            minion = player.set_aside_nemesis_sets.FindCard(name="Enraged Symbiote", card_type=Minion)
            if minion:
                first_player = Worlds.GetFirstPlayer(effect)
                minion.PutIntoPlay(first_player, effect)
            else:
                this.GainSurge(1, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Flash Thompson and take 2 damage → discard this obligation",
                lambda targets:
                    Faces.DiscardAll([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity"))
            .SetCostFunc(CostFunc.TakeDamage(2, "YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Put 1 set-aside copy of Enraged Symbiote into play engaged with the first player. If you cannot, this card gains surge. Discard this obligation",
                action
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            struggle_for_control
        )
    ]

