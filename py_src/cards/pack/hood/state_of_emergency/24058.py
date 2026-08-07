from . import *

# Hot Pursuit

def GetAbilities() -> Sequence['Ability']:

    def hot_pursuit_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion,
        )
        if face:
            face.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hot_pursuit_revealed
        ),
    ]

