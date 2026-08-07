from . import *

# * Ball and Chain

def GetAbilities() -> Sequence['Ability']:

    def ball_and_chain(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name=THUNDERBALL)
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            CardFinder(name=THUNDERBALL),
            ball_and_chain
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Exhaust("YourHero"))
        .SetCostFunc(CostFunc.Discard("RandomHandCard"))
    ]

