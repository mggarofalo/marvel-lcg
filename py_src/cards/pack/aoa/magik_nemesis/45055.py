from . import *

# Ruler of Limbo

def GetAbilities() -> Sequence['Ability']:

    def ruler_of_limbo_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        player = Worlds.FindPlayerByName("Illyana Rasputin", effect)
        if player:
            face = Find.Find(
                effect,
                who_perform=player,
                name="Limbo",
                card_type=Support
            )
            if face:
                this.PlaceCardHere(face, False, effect)

    def ruler_of_limbo(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        for face in this.GetPlacedCardArea().Get():
            player = face.GetOwner()
            if isinstance(player, Player) and face.IsName("Limbo"):
                face.PutIntoPlay(player, effect)

        Faces.DiscardAll(this.GetPlacedCardArea().Get(), effect)


    return [
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            "This",
            while_face_is_in_play=CardFinder(name="Belasco")
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            ruler_of_limbo_revealed
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.Temp0,
            "This",
            ruler_of_limbo
        ),
    ]

