from cards.pack import *

def BoardMember():

    def chief_medical_officer(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        this.card.Flip(effect)

    return AbilityFactory.IfThereAreAtLeastCounterHere(
        lambda effect: 3 if Worlds.IsExpert(effect) else 4,
        'secret',
        chief_medical_officer
    )

def IfThereAre3BoardMemberAttachmentsInPlayPlayersLoseTheGame():

    def action(effect: 'Effect', message: 'Message.AfterCardFlip'):
        if Worlds.FindCardSizeOnField(effect, CardFinder2("BOARD MEMBER", Attachment)) >= 3:
            Worlds.SetGameOver(False, effect)

    return AbilityFactory.AfterFlipToThisFace(
        AbilityType.NonKeyword,
        action
    )

