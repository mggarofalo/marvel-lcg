from cards.pack import *

def MisterSinister(level: int):
    def mister_sinister_revealed(effect: 'Effect', message: 'Message.AfterStatusCardPlaceOn') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        this.PlaceThreatOnSchemes("MainScheme", level, effect)

    return AbilityFactory.AfterStatusCardPlaceOn(
        AbilityType.ForcedResponse,
        "This",
        None,
        mister_sinister_revealed
    )

def FindMisterSinister(effect: 'Effect') -> 'Villain|None':
    return Worlds.FindCardOnField(
        effect,
        name="Mister Sinister",
        card_type=Villain
    )

def MainSchemeWhenRevealed(name: str, set_name: "CardFace.SETNAMES"):
    def revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        villain = FindMisterSinister(effect)
        if villain:
            face = Worlds.AsideDeck(effect).FindCard(name=name, card_type=Attachment)
            if face:
                face.AttachTo2(villain, effect)

            faces = Worlds.AsideDeck(effect).FindCards(set_name=set_name)
            Faces.ShuffleAllTo(faces, "EncounterDeck", effect)

    return AbilityFactory.WhenThisRevealed(
        None,
        revealed
    )

def MainSchemeWhenCompleted() -> List['Ability']:

    return []

    def completed(effect: 'Effect', message: 'Message.WhenMainSchemeStageCompleted') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        this.Advance()

    return AbilityFactory.WhenMainSchemeStageCompleted(
        "This",
        completed,
    )

