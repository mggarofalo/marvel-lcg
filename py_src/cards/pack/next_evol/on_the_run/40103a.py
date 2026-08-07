from . import *

# Gotta Get Away

def GetAbilities() -> Sequence['Ability']:

    def gotta_get_away(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        villains = Worlds.AsideDeck(effect).FindCards(card_type=Villain, trait="MARAUDER")
        villain = Rand.RandomChoice(villains, effect)
        villain.PutIntoPlay("FirstPlayer", effect)

        minion = Worlds.GetEncounterDeck(effect).FindCard(name=villain.name, card_type=Minion)
        if minion:
            Faces.RemoveAllFromGame([minion], effect)

        face = Worlds.AsideDeck(effect).FindCard(name="Hope's Captor", card_type=Attachment)
        if face:
            face.AttachTo2(villain, effect)

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            gotta_get_away
        ),
    ]

