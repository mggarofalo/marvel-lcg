from . import *

# Badoon Grunt

def GetAbilities() -> Sequence['Ability']:

    def badoon_grunt_revealed(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.engaged_player
        player.DealEncounterCards(1, effect)

    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "You",
            badoon_grunt_revealed,
            conditions=[
                lambda effect, message:
                    message.engaged_player.engaged_minions.GetSize() == 1
            ]
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay,
        ),
    ]

