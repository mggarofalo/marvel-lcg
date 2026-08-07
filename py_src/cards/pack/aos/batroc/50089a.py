from . import *

# Extract Captives

def GetAbilities() -> Sequence['Ability']:

    def extract_captives_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        env = GetAlertLevel(effect)
        if env:
            if env.HasTrait("HIGH"):
                Players.ForEachPlayer(effect, lambda player: player.DealEncounterCards(1, effect))
            else:
                Faces.RemoveTokensOn([env], "All", 'threat', effect)
                env.FlipTo(effect, trait="HIGH")

            if Worlds.IsExpert(effect):
                Faces.PlaceTokensOn([env], "2*", 'threat', effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            extract_captives_revealed
        ),
    ]

