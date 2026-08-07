from . import *

# The Show Must Go On

def GetAbilities() -> Sequence['Ability']:

    def the_show_must_go_on_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        env = Worlds.FindCardOnField(
            effect,
            trait="SHOW",
            card_type=Environment
        )
        if env:
            def action(player: 'Player'):
                face = Search.EncounterCard(
                    effect,
                    player,
                    include_discard_pile=True,
                    set_name=env.paper.set_name
                )
                if face:
                    player.DealEncounterCard(face, effect)
            Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_show_must_go_on_revealed
        ),
    ]

