from . import *

# Shield Toss

def GetAbilities() -> Sequence['Ability']:

    def shield_toss(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)

        discard_num = len(effect.cost_func.Get(CostFunc.Discard).return_discarded_cards)
        this.DealDamage(effect.targets[:discard_num], 4, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            shield_toss,
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy, range=(1, "All"))
        .SetCostFunc(CostFunc.Discard("YourHandCards", range=(0, "All")))
        .SetCostFunc(CostFunc.ReturnToHand(
            card_type=Upgrade,
            name=CAPTAIN_AMERICAS_SHIELD,
            from_where=["YouControlCards"],
            to_who="Initiator"
        )),
    ]

