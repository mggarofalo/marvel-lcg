from . import *

# Hydra Prison

def GetAbilities() -> Sequence['Ability']:

    def hydra_prison(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            face = Search.SearchForCard(
                effect,
                player,
                include_player_deck=True,
                include_player_discard_pile=True,
                include_player_hand_cards=True,
                card_type=Ally,
                card_class="IdentitySpecific",
            )
            if face:
                this.PlaceCardHere(face, False, effect)
                return FacesCounter.GetTotalCost([face])
            return 0

        value = Players.ForEachPlayer(effect, action, 0)

        this.PlaceThreatOnSchemes([this], value, effect)

    def hydra_prison_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        allies = this.GetPlacedCardArea().Get()

        Faces.MoveAllToProcessingArea(allies, effect)
        Faces.RemoveAllFromGame([this], effect)
        Faces.ReturnToHand(allies, "Owner", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hydra_prison
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            hydra_prison_defeated
        )
    ]

