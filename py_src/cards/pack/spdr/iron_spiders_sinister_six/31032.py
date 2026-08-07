from . import *

# * Electro

def GetAbilities() -> Sequence['Ability']:

    def electro(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer|Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        faces = CardFinder(has_printed_res="Y").Checks(player.hand_cards.GetAll())
        face = player.AskChooseFace(faces, effect)
        if face:
            this.PlaceCardHere(face, "Peek", effect)


    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.GetPlacedCardArea().FindCardSize(CardFinder(has_printed_res="Y")),
            health=1,
            change_on_event=OnEvent.PlacedCard("Here")
        ),
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "You",
            electro
        ),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            electro
        ),
    ]

