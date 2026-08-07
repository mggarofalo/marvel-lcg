from . import *

# Magnetic Mayhem

def GetAbilities() -> Sequence['Ability']:

    def magnetic_mayhem(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        # player = message.GetDefeatingPlayer()

        faces = Worlds.DiscardEncounterCards(4, effect)
        faces = CardFinder2("MAGNETIC").Checks(faces)

        scheme = Worlds.FindMainScheme(effect)
        if scheme:
            Faces.PlaceCountersOn([scheme], len(faces), 'magnet', effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            magnetic_mayhem,
            has_defeating_player=True
        ),
    ]

