from . import *

# Trance of Greed

def GetAbilities() -> Sequence['Ability']:

    def trance_of_greed_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        if not Faces.GiveStatus([identity], "Confused", effect):
            identity.TakeDamage(this, 2, effect)

    def trance_of_greed(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        faces = player.DrawUp(1, effect)
        if not player.PlayCardsLikeInTurn(faces, effect):
            identity.TakeDamage(this, 1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            trait="ENTHRALLED"
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            trance_of_greed_revealed
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.ForcedAction,
            trance_of_greed
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

