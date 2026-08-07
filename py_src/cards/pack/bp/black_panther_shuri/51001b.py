from . import *

# * Shuri

def GetAbilities() -> Sequence['Ability']:

    def shuri(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            finder=CardFinder(traits=["BLACK PANTHER", "TECH"]),
            card_type=Upgrade,
            # not_move=True
        )
        if face:
            initiator.PlayCardsLikeInTurn([face], effect, update_resources_cost=-2)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            shuri
        ).SetName("Inventor")
        .SetCostFunc(CostFunc.Exhaust("This"))
        .LimitOncePerRound(),
    ]

