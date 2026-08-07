from . import *

# The Towering Citadel

def GetAbilities() -> Sequence['Ability']:

    def the_towering_citadel(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)

        RevealRandomSetasidePRELATEMinion(first_player, effect)

        def action(player: 'Player'):
            if player != first_player:
                player.DealEncounterCards(1, effect)

        Players.ForEachPlayer(effect, action)

        SetupCards.Reveal(
            effect,
            name="The Tyrant's Throne",
            card_type=SchemeSide2
        )
        Faces.RemoveAllFromGame([this], effect)


    return [
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            "This",
            while_face_is_in_play=CardFinder2("PRELATE", Minion)
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            the_towering_citadel,
        ),
    ]

