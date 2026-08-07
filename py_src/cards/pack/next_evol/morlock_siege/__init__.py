from cards.pack import *

def IfThereAre3VillainsUnderRoutedThePlayersWinTheGame() -> 'Ability':

    def action(effect: 'Effect', message: 'Message2') -> None:
        face = Worlds.FindCardOnField(
            effect,
            name="Routed",
            card_type=Environment
        )
        if face:
            if len(face.GetPlacedCardArea().FindCards(card_type=Villain)) >= 3:
                Worlds.SetGameOver(True, effect)

    return AbilityFactory.AfterCardsMoved(
        AbilityType.NonKeywordBold,
        Villain,
        action
    )

def GetVillainsUnderRouted(by_effect: 'Effect') -> List['Villain']:
    face = Worlds.FindCardOnField(
        by_effect,
        name="Routed",
        card_type=Environment
    )
    if face:
        return face.GetPlacedCardArea().FindCards(card_type=Villain)
    else:
        return []

def Routed() -> 'Ability':

    def routed(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        this.PlaceCardHere(message.trigger, True, effect)
        # Hack
        message.trigger.FlipTo(effect, face_up=False)

        scenario = Worlds.GetScenario(effect)
        villain = scenario.villain_deck.Get()[0]

        minions = Worlds.FindCardsOnField(
            effect,
            name=villain.name,
            card_type=Minion
        )
        Faces.DiscardAll(minions, effect)

        villain.PutIntoPlay("FirstPlayer", effect)

        def action(player: 'Player'):
            villain.DoActivate(player, effect)
        Players.ForEachPlayer(effect, action)


    return AbilityFactory.AfterUnitBeDefeated(
        AbilityType.ForcedResponse,
        Villain,
        routed
    )

