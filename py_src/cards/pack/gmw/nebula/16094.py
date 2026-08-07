from . import *

# Cutthroat Ambition

def GetAbilities() -> Sequence['Ability']:

    def cutthroat_ambition(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Nebula")
        ),
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            CardFinder(name="Nebula"),
            cannot_take_more_than_damage=5,
            is_from_attack=True
        ),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            cutthroat_ambition
        ),
        AfterThisActivationEndsAttachThisCardToNebulaAndResolveItsSpecialAbility(),
    ]

