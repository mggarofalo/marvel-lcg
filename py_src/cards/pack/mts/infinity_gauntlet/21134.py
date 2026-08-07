from . import *

# Space Stone

def GetAbilities() -> Sequence['Ability']:

    def space_stone_revealed(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.GetToPlayer()
        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion,
        )
        if face and player:
            face.PutIntoPlay(player, effect)

        PlaceThisCardInTheInfinityStoneDeckDiscardPile(effect)



    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            space_stone_revealed
        ),
    ]

