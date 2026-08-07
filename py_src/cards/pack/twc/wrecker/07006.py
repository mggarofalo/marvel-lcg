from . import *

# * Magic Crowbar

def GetAbilities() -> Sequence['Ability']:

    def magic_crowbar(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        scheme = Worlds.FilterSideScheme(effect, least_threat=True)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name=WRECKER)
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            CardFinder(name=WRECKER),
            magic_crowbar
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Exhaust("YourHero"))
        .SetCostFunc(CostFunc.Discard("RandomHandCard"))
    ]

