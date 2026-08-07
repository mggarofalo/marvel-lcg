from . import *

# Upgrading Adaptoids

def GetAbilities() -> Sequence['Ability']:

    def upgrading_adaptoids(effect: 'Effect', message: 'Message.WhenMainSchemeStageWouldBeCompleted') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        message.SetBeInstead(effect)

        faces = Worlds.GetSetAsideAreaCards(
            effect,
            CardFinder2("ADAPTOID", Environment)
        )

        face = Rand.RandomChoice(faces, effect)
        face.PutIntoPlay("FirstPlayer", effect)

        if len(faces) == 1:
            Worlds.SetGameOver(False, effect)
        else:
            this.RemoveThreatFromSchemes([this], "All", effect)
            faces = Worlds.GetEncounterDiscardPileCards(effect, CardFinder(name="Adaptoid", card_type=Minion))
            Faces.ShuffleAllTo(faces, "EncounterDeck", effect)

    return [
        AbilityFactory.WhenMainSchemeStageWouldBeCompleted(
            AbilityType.ForcedInterrupt,
            "This",
            upgrading_adaptoids
        ),
    ]

