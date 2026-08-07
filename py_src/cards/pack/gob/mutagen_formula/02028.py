from . import *

# Overrun

def GetAbilities() -> Sequence['Ability']:

    def overrun(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            faces = Worlds.DiscardEncounterCards(2, effect)
            for face in faces:
                if face.HasTrait('GOBLIN') and Minion.IsType(face):
                    face.PutIntoPlay(player, effect)


        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            overrun
        ),
    ]

