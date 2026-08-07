from . import *

# Mutant Massacre

def GetAbilities() -> Sequence['Ability']:

    def mutant_massacre_lose(effect: 'Effect', message: 'Message.AfterCardLeavePlay') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        Worlds.SetGameOver(False, effect)

    def mutant_massacre(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        Faces.ShuffleAllTo(effect.targets, "EncounterDeck", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            mutant_massacre
        ).SetCostFunc(CostFunc.Exhaust(trait="MORLOCK", card_type=Ally))
        .SetTarget(Treachery, name="Hide!", from_where=["EncounterDiscardPile"]),
        AbilityFactory.AfterCardLeavePlay(
            AbilityType.NonKeywordBold,
            CardFinder(name="Morlock", card_type=Ally),
            mutant_massacre_lose,
            conditions=[
                lambda effect, message:
                    Worlds.FindCardOnField(
                        effect,
                        name="Morlock",
                        card_type=Ally
                    ) == None
            ]
        ),
        IfThereAre3VillainsUnderRoutedThePlayersWinTheGame(),
        AbilityFactory.IfThisSchemeStageIsCompletedPlayersLoseTheGame()
    ]

