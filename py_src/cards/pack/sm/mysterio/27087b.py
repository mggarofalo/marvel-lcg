from . import *

# Maze of Mirrors

def GetAbilities() -> Sequence['Ability']:

    def maze_of_mirrors(effect: 'Effect', message: 'Message.WhenPlayerWouldDrawCard|Message.WhenCardWouldDiscard') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = message.trigger.card.area.GetOwner()
        if isinstance(player, Player):
            message.SetBeInstead(effect)

            player.DealEncounterCard(message.trigger, effect)
            player.DrawUp(1, effect)

    return [
        AbilityFactory.WhenPlayerWouldDrawCard(
            AbilityType.ForcedInterrupt,
            "AnyPlayer",
            EncounterCard,
            maze_of_mirrors,
            conditions=[
                lambda effect, message:
                    message.trigger.card.area.flags.is_player_deck,
            ]
        ),
        AbilityFactory.WhenCardWouldDiscard(
            AbilityType.ForcedInterrupt,
            EncounterCard,
            maze_of_mirrors,
            from_player_deck=True
        ),
    ]

