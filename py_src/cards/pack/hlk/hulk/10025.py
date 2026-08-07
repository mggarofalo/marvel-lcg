from . import *

# Inner Demons

def GetAbilities() -> Sequence['Ability']:

    def inner_demons(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        identity = player.GetIdentity()
        identity.ChangeToOtherIdentityForm(effect)

        identity = identity.card.face

        if identity.IsName("Bruce Banner"):
            player.DiscardHandCards(("Zero", 2), effect)
            Faces.DiscardAll([this], effect)
        elif identity.IsName("Hulk"):
            Faces.ExhaustAll([identity], effect)
            Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            inner_demons
        ),
    ]

