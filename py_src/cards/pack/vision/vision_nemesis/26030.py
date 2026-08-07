from . import *

# Ultron Unleashed

def GetAbilities() -> Sequence['Ability']:

    def ultron_unleashed_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            include_set_aside=True,
            name="Ultron Drones",
            card_type=Environment
        )
        if face:
            face.PutIntoPlay(player, effect)

        def action(player: 'Player'):
            CreateUltronFacedownDrone(player, 1, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ultron_unleashed_revealed
        ),
    ]

