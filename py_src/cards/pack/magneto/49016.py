from . import *

# Won't Stay Down

def GetAbilities() -> Sequence['Ability']:

    def wont_stay_down(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ReturnToHand(effect.targets, initiator, effect)


    return [
        AbilityFactory.CanPlayThisSupportCard(
        ).SetPlay(only_if_your_identity_has_one_of_traits=["X-FORCE", "X-MEN"]),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            wont_stay_down
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(Ally, traits=["X-FORCE", "X-MEN"], from_where=["YourDiscardPile"]),
    ]

