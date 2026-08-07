from . import *

# The Crazy Gang

def GetAbilities() -> Sequence['Ability']:

    def the_crazy_gang(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetAgainstPlayer()

        if player:
            face = message.trigger
            player.DealEncounterCard(face, effect)

            if Worlds.GetPlayerNum(effect) > 1:
                player = Worlds.GetNextPlayer(player, effect)
                if player:
                    player.DealEncounterCard(face, effect)


    return [
        AbilityFactory.AfterUnitSchemeAgainst(
            AbilityType.ForcedResponse,
            NonEliteMinion,
            "AnyPlayer",
            the_crazy_gang,
        ),
    ]

