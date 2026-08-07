from . import *

# * Misty Knight

def GetAbilities() -> Sequence['Ability']:

    def misty_knight(effect: 'Effect', message: 'Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = initiator.LookAtDeck("EncounterDeck", 2, effect)
        face = initiator.AskDiscardFace(faces, effect)

        if face:
            value = FacesCounter.CountTotalBoostStarAndBoost([face])
            message.GainTHWForThisThwart(value, effect)


    return [
        AbilityFactory.WhenUnitWouldThwart(
            AbilityType.Interrupt,
            "This",
            misty_knight
        ),
    ]

