from . import *

# Common Criminal

def GetAbilities() -> Sequence['Ability']:

    def common_criminal_boost(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        identity.DealDamage([this], 3, effect)

        if this.IsDefeated():
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Draw 1 card",
                    lambda targets:
                        Players.DrawUp(targets, 1, effect)
                ).SetTarget("YourIdentity"),
                AbilityFactory.ForChoiceAbility(
                    "Remove 3 threat",
                    lambda targets:
                        identity.RemoveThreatFromSchemes(targets, 3, effect)
                ).SetTarget(SchemeSide2),
            )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            common_criminal_boost
        ).SetCost(Cost("R")),
    ]

