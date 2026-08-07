from . import *

# Team Strike

def GetAbilities() -> Sequence['Ability']:

    def team_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetHero()
        damage = identity.attack
        for face in effect.cost_func.Get(CostFunc.Exhaust, index=1).return_exhausted_cards:
            if HasAttack.IsType(face):
                damage += face.attack
        this.DealDamageTotal(effect.targets, damage, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            team_strike
        ).SetPlay(only_if_your_identity_has_trait="X-MEN").SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust("YourHero"))
        .SetCostFunc(CostFunc.Exhaust(card_type=Ally, trait="X-MEN", range=(0, "All")))
        # .SetCostFunc(CostFunc.Exhaust(card_type=Hero|Ally, trait="X-MEN"))
        .SetTarget(Enemy, range=(
            1,
            lambda effect:
                sum([x.attack for x in effect.GetInitiator().GetControlAllies(CardFinder2("X-MEN"))]) + \
                effect.GetInitiator().GetHero().attack
        ), repeat_rules="Health"),
    ]

