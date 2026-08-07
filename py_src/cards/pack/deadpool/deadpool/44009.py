from . import *

# Armed to the Teeth

def GetAbilities() -> Sequence['Ability']:

    def armed_to_the_teeth_play(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.Collection(effect, initiator, trait="WEAPON", card_type=Upgrade, card_classes="Any")
        if face:
            this.PlaceCardHere([face], False, effect)

    def armed_to_the_teeth(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        here_face = this.GetPlacedCardArea().Get(True)[0].CastTo(Asset2)

        face = effect.targets[0]
        area = face.card.area
        this.PlaceCardHere(face, False, effect)

        assert area.bind_card, f"{area=}"
        here_face.AttachTo2(area.bind_card.face, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            "This",
            armed_to_the_teeth_play
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            armed_to_the_teeth,
            conditions=[
                lambda effect, message:
                    effect.this.GetPlacedCardArea().GetSize() > 0
            ],
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Upgrade, trait="WEAPON", from_where=["YouControlCards"]),
    ]

