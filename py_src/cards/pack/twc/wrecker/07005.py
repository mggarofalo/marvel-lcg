from . import *

# Held Hostage

def GetAbilities() -> Sequence['Ability']:

    def held_hostage(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        initiator = effect.GetInitiator()

        villain = this.GetBindFace().CastTo(EncounterSideScheme).FindCorrespondingVillain()
        if villain:
            villain.DoAttackYou(initiator, effect)

        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            SchemeSide2,
            corresponding="ActiveVillain"
        ),
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            "AttachedScheme",
            by_thwarting=True,
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            held_hostage
        )
    ]

