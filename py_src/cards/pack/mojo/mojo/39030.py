from . import *

# Paparazzi

def GetAbilities() -> Sequence['Ability']:
    def paparazzi_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)
        player = this.GetGaveToPlayer()

        def remove_threat_from_here(targets: Sequence['CardFace']):
            if player.IsAlterEgo():
                num = 3
            else:
                num = 2
            Faces.RemoveTokensOn([this], num, 'threat', effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust a character you control",
                remove_threat_from_here
            ).SetCostFunc(CostFunc.Exhaust("YouControlUnit")),
            AbilityFactory.ForChoiceAbility(
                "Discard 1 card from your hand",
                remove_threat_from_here
            ).SetCostFunc(CostFunc.Discard("YourHandCards")),
        )

    def paparazzi(effect: 'Effect', message: 'Message.WhenPlayerTurnEnd') -> None:
        this = effect.this.CastTo(Obligation)

        num = Faces.RemoveTokensOn([this], "All", 'threat', effect)
        if num:
            this.PlaceThreatOnSchemes("MainScheme", num, effect)
        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            paparazzi_action
        ),
        AbilityFactory.WhenPlayerTurnEnd(
            AbilityType.ForcedInterrupt,
            "You",
            paparazzi
        )
    ]

