from . import *

# The Mad Doctor - 2A

def GetAbilities() -> Sequence['Ability']:

    def the_mad_doctor(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                card_type=Minion,
            )
            if face:
                face.Reveal(player, effect)

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_mad_doctor
        ),
    ]

