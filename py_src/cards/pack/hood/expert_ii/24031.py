from . import *

# Seek and Destroy

def GetAbilities() -> Sequence['Ability']:

    def seek_and_destroy(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

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


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            seek_and_destroy
        ),
    ]

