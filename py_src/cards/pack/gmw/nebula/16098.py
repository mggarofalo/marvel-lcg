from . import *

# Wide Stance

def GetAbilities() -> Sequence['Ability']:

    def wide_stance(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        if player:
            player.DiscardRandomHandCards(1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Nebula")
        ),
        AbilityFactory.ReduceCharacterTakeDamageBy(
            CardFinder(name="Nebula"),
            1,
            from_each_attack=True
        ),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            wide_stance
        ),
        AfterThisActivationEndsAttachThisCardToNebulaAndResolveItsSpecialAbility(),
    ]

