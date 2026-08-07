from . import *

# Noble Sacrifice

def GetAbilities() -> Sequence['Ability']:

    def noble_sacrifice(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = 0
        for face in effect.cost_func.Get(CostFunc.Discard).return_discarded_cards:
            if Ally.IsType(face):
                value += face.printed_health

        this.HealthUnits(effect.targets, value, effect)
        Faces.GiveStatus(effect.targets, "Tough", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            noble_sacrifice
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Discard("YourAlly"))
        .SetTarget("YourHero",
            finder=CardFinder(canbe_heal=True)|CardFinder(canbe_tough=True)
        ),
    ]

