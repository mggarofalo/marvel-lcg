from . import *

# Draugr Buddy

def GetAbilities() -> Sequence['Ability']:

    def draugr_buddy(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetDefeatingPlayer()
        face = Worlds.DiscardEncounterTopCard(effect)
        if Treachery.IsType(face):
            player.DealEncounterCard(this, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            draugr_buddy
        ),
    ]

