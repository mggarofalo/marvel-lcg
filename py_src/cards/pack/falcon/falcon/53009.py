from . import *

# Aerial Recon

def GetAbilities() -> Sequence['Ability']:

    def aerial_recon(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'recon', effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            aerial_recon
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.DealPlayerEncounterCard(1, "AnyPlayer")),
        AbilityFactory.WhenPlayerWouldBeDealtEncounterCard(
            AbilityType.Interrupt,
            lambda effect, message:
                message.SetBeInstead(effect)
        ).SetCostFunc(CostFunc.Counter("This", 1, 'recon'))
    ]

