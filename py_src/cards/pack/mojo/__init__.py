from cards.pack import *

def DiscardEachSettingAndGainSurge() -> 'Ability':

    def mojo_in_the_middle_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Worlds.DiscardEachOther(
            effect,
            this,
            trait="SETTING",
            card_type=Environment
        )
        if message.IsFromEncounterDeck():
            ThisCardGainSurge(effect)

    return AbilityFactory.WhenThisRevealed(
        None,
        mojo_in_the_middle_revealed
    )

