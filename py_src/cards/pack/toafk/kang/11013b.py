from . import *

# Kang's Wrath - 4B

def GetAbilities() -> Sequence['Ability']:

    def kangs_wrath(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                include_set_aside=True,
                card_type=Minion,
                is_nemesis=player,
            )
            if face:
                face.PutIntoPlay(player, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            kangs_wrath
        )
    ]

