from . import *

# * Abyss

def GetAbilities() -> Sequence['Ability']:

    def abyss(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        face = player.player_deck.GetTop()
        if face:
            this.PlaceCardHere(face, False, effect)


    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.GetPlacedCardArea().FindCardSize(CardFinder(is_face_up=False)),
            health=2,
            change_on_event=OnEvent.PlacedCard("Here"),
        ),
        AbilityFactory.ThisMinionEngageFirstPlayer(),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            abyss,
        ),
    ]

