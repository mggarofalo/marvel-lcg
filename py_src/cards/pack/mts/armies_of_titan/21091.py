from . import *

# Landing Craft

def GetAbilities() -> Sequence['Ability']:

    def landing_craft(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.defeating_player

        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion,
        )
        if face and player:
            face.PutIntoPlay(player, effect)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            landing_craft,
        ),
    ]

