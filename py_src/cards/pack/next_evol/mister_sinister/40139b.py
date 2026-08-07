from . import *

# Sinister Intent

def GetAbilities() -> Sequence['Ability']:

    def sinister_intent_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        faces = Worlds.MainSchemesDeck(effect).FindCards(stage=2, card_type=MainScheme)
        face = Rand.RandomChoice(faces, effect)
        Faces.RemoveAllFromGame([face], effect)

        Rand.Shuffle(faces, effect)
        this.Advance(faces[0], effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sinister_intent_revealed
        ),
    ]

