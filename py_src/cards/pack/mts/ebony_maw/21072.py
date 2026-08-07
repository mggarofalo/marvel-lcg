from . import *

# * Ebony Maw

def GetAbilities() -> Sequence['Ability']:

    def ebony_maw_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(player: 'Player'):
            face = Worlds.DiscardEncounterCardsUntil(
                effect,
                trait="SPELL",
            )
            if face:
                face.PutIntoPlay(player, effect)
        Players.ForEachPlayer(effect, action)


    return [
        WhenEbonyMawActivatesAgainstYouRemoveAnInvocationCounterFromEachSpellCardInYourPlayArea(),
        AbilityFactory.WhenThisRevealed(
            None,
            ebony_maw_revealed
        ),
    ]

