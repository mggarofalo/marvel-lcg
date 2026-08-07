from cards.pack import *

def PlaceCharmCounter(player: 'Player', size: int, effect: 'Effect'):
    faces = Filter.ByFinder(player.GetPlayerAreaCards(), CardFinder(trait="ENCHANTMENT"))
    Faces.PlaceCountersOn(faces, size, 'charm', effect)

def AfterEnchantressAttacksYouPlaceCharmCounter() -> 'Ability':
    def enchantress(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            PlaceCharmCounter(player, 1, effect)


    return AbilityFactory.AfterUnitAttackYou(
        AbilityType.ForcedResponse,
        "This",
        enchantress
    )

