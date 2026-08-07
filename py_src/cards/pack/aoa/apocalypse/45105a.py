from . import *

# The Tyrant's Throne

def GetAbilities() -> Sequence['Ability']:

    def the_tyrants_throne(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)

        RevealRandomSetasidePRELATEMinion(first_player, effect)

        def action(player: 'Player'):
            if player != first_player:
                player.DealEncounterCards(1, effect)

        Players.ForEachPlayer(effect, action)

        this.card.Flip(effect)
        SetupCards.Reveal(
            effect,
            name="No Longer Worthy"
        )

    return [
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            "This",
            while_face_is_in_play=CardFinder2("PRELATE", Minion)
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            the_tyrants_throne,
        ),
    ]

