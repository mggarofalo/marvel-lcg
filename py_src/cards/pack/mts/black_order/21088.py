from . import *

# Blood to Spare

def GetAbilities() -> Sequence['Ability']:

    def blood_to_spare_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(player: 'Player'):
            if not player.GetEngagedMinions():
                face = Search.EncounterCard(
                    effect,
                    player,
                    include_discard_pile=True,
                    trait="BLACK ORDER",
                    card_type=Minion
                )
                if face:
                    face.PutIntoPlay(player, effect)
            else:
                Worlds.Enemies.EngagedMinionActivateAgainstYou(effect, player)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            blood_to_spare_revealed
        ),
    ]

