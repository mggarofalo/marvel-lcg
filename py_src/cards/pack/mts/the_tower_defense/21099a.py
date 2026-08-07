from . import *

# The Armies of Thanos

def GetAbilities() -> Sequence['Ability']:

    def the_armies_of_thanos_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        face = SetupCards.PutIntoPlay(
            effect,
            name="Avengers Tower",
            card_type=Environment
        )
        if face:
            face.FlipTo(effect, trait="STRONGHOLD")

        SetupCards.AttachTo(
            effect,
            attach_to=this,
            name="Focused Defense",
            card_type=Attachment
        )

        def action(player: 'Player'):
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=False,
                name="Black Order Besieger",
                card_type=Minion,
            )
            if face:
                face.PutIntoPlay(player, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_armies_of_thanos_revealed
        ),
    ]

