from cards.pack import *

# Test Ally 001

def GetAbilities() -> Sequence['Ability']:

    def remove_2_threat(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)
        this.RemoveThreatFromSchemes(effect.targets, 2, effect)

    def draw_up_cards(effect: 'Effect', message: 'Message.AfterCardLeavePlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.GetControlByPlayer().DrawUp(4, effect)

    def deal_damage(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect)

    return [
        # Ability 1: After enters play, spend a Blue resource and then remove 2 threats
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            remove_2_threat
        ).SetTarget(Scheme2)
        .SetCost(Cost("B")),
        # Ability 2: After this leave play, exhaust your identity and draw 2 cards
        AbilityFactory.AfterCardLeavePlay(
            AbilityType.Response,
            "This",
            draw_up_cards
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
        # Ability 3: Action, exhaust this card and deal 4 damage to an enemy
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            deal_damage
        ).SetTarget(Enemy)
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetName("Deal 4 damage"),
    ]

