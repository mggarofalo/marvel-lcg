from . import *

# Wheel of Genres

def GetAbilities() -> Sequence['Ability']:

    def wheel_of_genres(effect: 'Effect', message: 'Message.WhenVillainPhaseStepStart') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        faces = AsideModularSet.ChooseRandom(effect)
        environments = CardFinder2("SHOW", Environment).Checks(faces)
        for face in environments:
            face.Reveal(None, effect)
            faces.remove(face)

        Faces.MoveAllTo(faces, Worlds.GetEncounterDeck(effect), effect)
        Worlds.GetEncounterDeck(effect).Shuffle(effect, only_for_most_top=len(faces))

        first_player = Worlds.GetFirstPlayer(effect)
        first_player.DealEncounterCards(2, effect)

        this.card.Flip(effect)


    return [
        AbilityFactory.WhenVillainPhaseStepStart(
            AbilityType.ForcedInterrupt,
            3,
            wheel_of_genres
        ),
    ]

