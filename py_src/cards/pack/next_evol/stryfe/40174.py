from . import *

# * Zero

def GetAbilities() -> Sequence['Ability']:

    def zero(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetKillerPlayer()
        if player:
            if FacesCounter.GetMaxSameTypeCount(player.hand_cards.Get()) < 3:
                Faces.ShuffleAllTo([this], "EncounterDeck", effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            zero
        ),
    ]

