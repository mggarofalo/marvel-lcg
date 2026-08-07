from . import *

# Contaminant Immunity

def GetAbilities() -> Sequence['Ability']:

    def contaminant_immunity(effect: 'Effect', message: 'Message.WhenStatusWouldCardPlaceOn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)
        Faces.DiscardAll([this], effect)

        Faces.GiveStatus([message.trigger], "Tough", effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Spider-Woman")
        ),
        AbilityFactory.WhenUnitWouldBeStatus(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Spider-Woman"),
            ["Stunned", "Confused"],
            contaminant_immunity
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

