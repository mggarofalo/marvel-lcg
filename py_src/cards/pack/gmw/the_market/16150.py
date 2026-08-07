from . import *

# Brainstorm

def GetAbilities() -> Sequence['Ability']:

    def brainstorm(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        card_type = initiator.DeclareCardType()
        faces = initiator.LookAtDeck(initiator.player_deck, 1, effect)
        if faces:
            face = faces[0]
            if card_type.IsType(face):
                this.RemoveThreatFromSchemes(effect.targets2, 3, effect)

            initiator.PlaceOnTopAndOrBottomInAnyOrder(faces, effect)
            initiator.DrawUp(1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            brainstorm
        ).SetPlay().SetLabel('thwart')
        .SetTarget2(MainScheme),
    ]

