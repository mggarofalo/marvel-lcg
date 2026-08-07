from . import *

# Evasive Maneuvering

def GetAbilities() -> Sequence['Ability']:

    def evasive_maneuvering(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        if player:
            identity = player.GetIdentity()
            if identity.IsStunned():
                villain = Worlds.FindCardOnField(effect, name="Nebula")
                if villain:
                    Faces.GiveFacedownBoostCards([villain], 1, effect)
            else:
                Faces.GiveStatus([identity], "Stunned", effect)


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
            evasive_maneuvering
        ),
        AfterThisActivationEndsAttachThisCardToNebulaAndResolveItsSpecialAbility(),
    ]

