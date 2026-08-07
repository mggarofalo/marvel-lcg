from . import *

# Superpower Adaptation

def GetAbilities() -> Sequence['Ability']:

    def superpower_adaptation(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        friend = IfTouchedIsAttachTo(effect, Friend)
        if friend:
            card_classes: List[ClassCard.CARD_CLASS] = []
            if ClassCard.IsType(friend):
                card_classes = friend.card_classes
            else:
                card_classes = ["IdentitySpecific"]

            face = Search.PlayerCard(
                effect,
                friend.GetOwnerPlayer(),
                include_player_deck=False,
                include_discard_pile=True,
                card_type=Event,
                card_classes=card_classes
            )
            if face:
                initiator = effect.GetInitiator()
                Faces.AddToHand([face], initiator, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            superpower_adaptation,
            conditions=[
                lambda effect, message:
                    TouchedAttachedTo(effect, Friend)
            ]
        ).SetPlay().SetLabel(),
    ]

