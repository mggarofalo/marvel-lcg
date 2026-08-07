from . import *

# Heart of the Empire

def GetAbilities() -> Sequence['Ability']:

    def heart_of_the_empire(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)

        RevealRandomSetasidePRELATEMinion(first_player, effect)

        def action(player: 'Player'):
            if player != first_player:
                player.DealEncounterCards(1, effect)

        Players.ForEachPlayer(effect, action)

        this.card.Flip(effect)

    return [
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            "This",
            while_face_is_in_play=CardFinder2("PRELATE", Minion)
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            heart_of_the_empire,
        ),
    ]

