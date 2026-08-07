from . import *

# * Silk: Cindy Moon

def GetAbilities() -> Sequence['Ability']:

    def silk(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.EncounterCard(
            effect,
            initiator,
            include_discard_pile=False,
            card_type=Treachery,
        )
        if face:
            Faces.DiscardAll([face], effect)

    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            silk,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().IsControl(CardFinder(trait="WEB-WARRIOR", check_face_fn=lambda face: face != effect.this))
            ]
        ),
    ]

