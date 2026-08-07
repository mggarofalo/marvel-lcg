from . import *

# Darkchilde

def GetAbilities() -> Sequence['Ability']:

    def darkchilde(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            this.DealDamage(targets, 1, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Illyana Rasputin → remove Darkchilde from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Deal 1 damage to each character you control. Discard this obligation",
                action
            ).SetTarget("YouControlUnit", range="All"),
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            darkchilde
        )
    ]

