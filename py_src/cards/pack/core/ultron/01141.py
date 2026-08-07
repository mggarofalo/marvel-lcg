from . import *

# Program Transmitter

def GetAbilities() -> Sequence['Ability']:

    def program_transmitter(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        for scheme in Worlds.GetSideSchemes(effect):
            this.PlaceThreatOnSchemes([scheme], 1, effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Ultron")
        ),
        AbilityFactory.AfterUnitSchemeEnd(
            AbilityType.ForcedResponse,
            CardFinder(name="Ultron"),
            program_transmitter
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("BB"))
        .SetCostFunc(CostFunc.Exhaust("YourHero")),
    ]

