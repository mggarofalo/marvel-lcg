from . import *

# Unyielding Persistence

def GetAbilities() -> Sequence['Ability']:

    def unyielding_persistence(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        villain = Worlds.FindCardOnField(effect, name="Nebula", card_type=Unit2)
        if villain:
            if villain.IsTough():
                Faces.GiveFacedownBoostCards([villain], 1, effect)
            else:
                Faces.GiveStatus([villain], "Tough", effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Nebula")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Nebula"),
            stalwart=1,
        ),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            unyielding_persistence
        ),
        AfterThisActivationEndsAttachThisCardToNebulaAndResolveItsSpecialAbility(),
    ]

