from . import *

# Captured by Hydra

def GetAbilities() -> Sequence['Ability']:

    def captured_by_hydra_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.GetSetAsideAreaCards(effect)
        faces = CardFinder2("CAPTIVE", Ally).Checks(faces)
        face = Rand.RandomChoice(faces, effect)

        if face:
            this.PlaceCardHere(face, False, effect)

            def gain_card_and_remove(defeated_effect: 'Effect', defeated_message: 'Message.WhenSchemeBeDefeated'):
                player = defeated_message.defeating_player
                if player:
                    player.GainCard(face, effect)
                    Faces.RemoveAllFromGame([this], effect)
                else:
                    Faces.SetAside([face], effect)

            this.effect.RegisterTemp(
                AbilityFactory.WhenSchemeBeDefeated(
                    AbilityType.Temp0,
                    "This",
                    gain_card_and_remove
                ),
                unregister_after_exec=True,
            )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            captured_by_hydra_revealed
        ),
    ]

