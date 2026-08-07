from . import *

# Weapon Mastery

def GetAbilities() -> Sequence['Ability']:

    def weapon_mastery(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        if player:
            player.GetIdentity().TakeDamage(this, 1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Nebula")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Nebula"),
            retaliate=1,
        ),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            weapon_mastery
        ),
        AfterThisActivationEndsAttachThisCardToNebulaAndResolveItsSpecialAbility(),
    ]

