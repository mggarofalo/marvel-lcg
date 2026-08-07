from . import *

# Erratic Teleportation

def GetAbilities() -> Sequence['Ability']:

    def erratic_teleportation_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        vilalin = Worlds.FindVillain(effect, name="Spiral")
        if vilalin:
            if vilalin.HasTrait("CORNERED"):
                Faces.PlaceCountersOn([vilalin], 1, 'teleport', effect)
            if vilalin.HasTrait("ESCAPED"):
                face = GetShowDeck(effect).GetTopCard()
                if face:
                    if player.AskSpendResources(Cost("B"), effect):
                        player.LookAtDeck(GetShowDeck(effect).deck, 1, effect)
                        ask = player.AskChooseOneText(["Top", "Bottom"])
                        if ask == "Bottom":
                            face.card.MoveToBottom(GetShowDeck(effect).deck, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            erratic_teleportation_revealed
        ),
    ]

