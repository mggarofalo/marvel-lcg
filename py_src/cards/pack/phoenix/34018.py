from . import *

# Mutant Peacekeepers

def GetAbilities() -> Sequence['Ability']:

    def mutant_peacekeepers(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetHero()
        value = identity.thwart
        for face in effect.cost_func.Get(CostFunc.Exhaust, index=1).return_exhausted_cards:
            if HasThwart.IsType(face):
                value += face.thwart
        this.RemoveThreatFromSchemesTotal(effect.targets, value, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            mutant_peacekeepers
        ).SetPlay(only_if_your_identity_has_trait="X-MEN").SetLabel('thwart')
        .SetCostFunc(CostFunc.Exhaust("YourHero"))
        .SetCostFunc(CostFunc.Exhaust(card_type=Ally, trait="X-MEN", range=(0, "All")))
        .SetTarget(Scheme2,
            range=(1,
                lambda effect:
                    effect.GetInitiator().GetHero().thwart + \
                    sum([x.thwart for x in effect.GetInitiator().GetControlAllies(CardFinder(canbe_exhaust=True, trait="X-MEN")) if Ally.IsType(x)])
            ), repeat_rules="Threat"),
    ]

