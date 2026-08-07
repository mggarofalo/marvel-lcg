from . import *

# * Hulkling

def GetAbilities() -> Sequence['Ability']:

    def hulkling(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = initiator.GetControlCards2(CardFinder2("SHAPESHIFT", Upgrade))
        if message.trigger in faces:
            faces.remove(message.trigger)
        Faces.DiscardAll(faces, effect)

        Faces.ReadyAll([this], effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.ForcedResponse,
            CardFinder(trait="SHAPESHIFT", card_type=Upgrade),
            hulkling,
            under_your_control=True
        ).SetName("Chosen Shape"),
    ]

