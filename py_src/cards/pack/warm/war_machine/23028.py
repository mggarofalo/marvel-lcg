from . import *

# Equipment Malfunction

def GetAbilities() -> Sequence['Ability']:

    def equipment_malfunction(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            identity = player.GetIdentity()
            size = Faces.RemoveCountersOn([identity], "All", 'ammo', effect)
            if not size or size <= 2:
                ThisCardGainSurge(effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust James Rhodes → remove Equipment Malfunction from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Remove all ammo counters from your identity. If 2 or fewer ammo counters were removed this way, this card gains surge. Discard this obligation",
                action
            ),
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            equipment_malfunction
        ),
    ]

