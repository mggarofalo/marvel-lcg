from . import *

# * Magneto

def GetAbilities() -> Sequence['Ability']:

    def magneto(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()

        discards_faces: List['CardFace'] = []

        face = initiator.DiscardDeckUntil(
            effect,
            trait="MAGNETIC",
            return_other_faces=discards_faces
        )
        if face:
            Faces.AddToHand([face], initiator, effect)
            discards_faces.append(face)

        this.GetBuff(Buff_49001a).Update(discards_faces)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            magneto
        ).SetName("Magnetic Pull")
        .LimitOncePerRound(),
    ]

