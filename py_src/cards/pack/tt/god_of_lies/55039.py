from . import *

# * Malekith

def GetAbilities() -> Sequence['Ability']:

    def malekith(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        face = Worlds.DiscardEncounterTopCard(effect)
        if Treachery.IsType(face) and player:
            player.DealEncounterCard(face, effect)


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            malekith
        ),
        WhenDefeatedPlaceShatterCountersOnTheAvatarOfLokivillain(2)
    ]

