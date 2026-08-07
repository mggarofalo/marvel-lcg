from cards.pack import *

def OdinAttachToMainScheme(effect: 'Effect') -> bool:
    return effect.this.bind_face != None and \
        MainScheme.IsType(effect.this.GetBindFace())
    # schemes = effect.world.GetAllMainSchemes()
    # for scheme in schemes:
    #     odin = scheme.components.inventory.GetDeck().FindCard(name="Odin", card_type=Ally)
    #     if odin:
    #         return True
    # return False

def WhenDefeatedYouWinIfOdinIsNotAttachedToTheMainScheme() -> 'Ability':
    def you_win(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        Worlds.SetGameOver(True, effect)

    return AbilityFactory.WhenUnitBeDefeated(
        AbilityType.NonKeywordBold,
        "This",
        you_win,
        conditions=[
            lambda effect, message:
                not OdinAttachToMainScheme(effect)
        ],
    )

# def TheFirstPlayerReveal(name: str, card_type: Type['TC'], effect: 'Effect'):
#     first_player = Worlds.GetFirstPlayer(effect)
#     first_player.SearchEncounterCard(
#         effect,
#         True,
#         lambda face: face.Reveal(first_player, effect),
#         name=name,
#         card_type=card_type
#     )

# def DealEachOtherPlayerOneFacedownEncounterCard(this: 'CardFace', effect: 'Effect'):
#     first_player = Worlds.GetFirstPlayer(effect)

#     for player in effect.world.GetPlayers(effect):
#         if player != first_player:
#             player.DealEncounterCards(1, effect)

def AttachThisToHela(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
    this = effect.this.CastTo(Attachment)
    Unused(this)

    hela = Worlds.FindVillain(effect)
    if hela:
        this.AttachTo2(hela, effect)

def GetSideSchemesNumInVictoryDisplay(effect: 'Effect') -> int:
    return Worlds.VictoryDisplay(effect).FindCardSize(
        CardFinder(card_type=SchemeSide2)
    )

