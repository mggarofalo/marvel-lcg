from . import *

# Apocalyptic Influence

def GetAbilities() -> Sequence['Ability']:

    def apocalyptic_influence(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        if player.GetIdentity().IsName("Archangel"):
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)
        else:
            player.GetIdentity().ChangeToForm(CardFinder(name="Archangel"), effect)


    def apocalyptic_influence_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)
        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.WhenThisObligationRevealed(
            apocalyptic_influence
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            apocalyptic_influence_action,
        ).SetCostFunc(CostFunc.DealPlayerEncounterCard(
            1, "FirstPlayer"))
    ]

