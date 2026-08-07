from . import *

# * Mark V Helmet

def GetAbilities() -> Sequence['Ability']:

    def mark_v_helmet(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)
        this.RemoveThreatFromSchemes(effect.targets, 1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            mark_v_helmet,
            conditions=[
                lambda effect, message:
                    not effect.GetInitiator().GetHero().HasTrait('AERIAL')
            ],
        ).SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetCostFunc(CostFunc.Exhaust("This")),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            mark_v_helmet,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetHero().HasTrait('AERIAL')
            ],
        ).SetLabel('thwart')
        .SetTarget(Scheme2, range="All")
        .SetCostFunc(CostFunc.Exhaust("This")),
    ]
