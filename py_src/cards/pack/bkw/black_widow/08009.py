from . import *

# * Synth-Suit

def GetAbilities() -> Sequence['Ability']:

    def synth_suit(effect: 'Effect', message: 'Message.WhenEffectWouldResolve') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Black Widow"),
            defense=1,
        ),
        AbilityFactory.AfterPlayerTriggerAbility(
            AbilityType.HeroResponse,
            "You",
            CardFinder2("PREPARATION"),
            synth_suit,
            card_control_by="You"
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Black Widow", canbe_ready=True),
    ]

