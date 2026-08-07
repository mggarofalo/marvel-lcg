from . import *

# Black Order Besieger

def GetAbilities() -> Sequence['Ability']:

    def black_order_besieger(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.engaged_player

        player.ChooseAbilities(
            effect,
            AbilityDealDamageToAvengersTower(1, effect),
            AbilityFactory.ForChoiceAbility(
                "Deal 2 damage to your identity",
                lambda targets:
                    this.DealDamage(targets, 2, effect)
            ).SetTarget("YourIdentity")
        )

    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "You",
            black_order_besieger
        ),
    ]

