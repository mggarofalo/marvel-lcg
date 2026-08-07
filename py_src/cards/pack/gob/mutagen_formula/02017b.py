from . import *

# Unleashing the Mutagen - 1B

def GetAbilities() -> Sequence['Ability']:

    def unleashing_the_mutagen(effect: 'Effect', message: 'Message.WhenMainSchemeStageCompleted') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            if not player.engaged_minions.FindCard(trait="GOBLIN", card_type=Minion):
                faces = Worlds.DiscardEncounterCards(3, effect)
                for face in faces:
                    if Minion.IsType(face) and face.HasTrait("GOBLIN"):
                        face.PutIntoPlay(player, effect)
                        break

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenMainSchemeStageCompleted(
            AbilityType.WhenCompleted,
            "This",
            unleashing_the_mutagen
        )
    ]

