from . import *

# MojoMania

def GetAbilities() -> Sequence['Ability']:

    def mojomania_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        faces = AsideModularSet.ChooseRandom(effect)
        environments = CardFinder2("SHOW", Environment).Checks(faces)
        for face in environments:
            face.Reveal(None, effect)
            faces.remove(face)

        Faces.ShuffleAllTo(faces, "EncounterDeck", effect)


    def mojomania(effect: 'Effect', message: 'Message.AfterCardLeavePlay|Message.WhenCardFlip') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        value = Faces.RemoveTokensOn([message.trigger], "All", 'threat', effect)
        if value:
            this.PlaceThreatOnSchemes([this], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mojomania_revealed
        ),
        AbilityFactory.WhenCardFlipOrLeavePlay(
            AbilityType.ForcedInterrupt,
            Unit2,
            mojomania,
        ),
    ]

