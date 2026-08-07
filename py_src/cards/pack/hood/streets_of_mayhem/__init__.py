from cards.pack import *

def WhenRevealedDiscardOtherSettingEnviroment() -> 'Ability':

    def discard_other_enviroment(effect: 'Effect', message: 'Message.WhenCardRevealed'):
        this = effect.this.CastTo(Environment)
        Unused(this)

        Worlds.DiscardEachOther(
            effect,
            this,
            trait="SETTING",
            card_type=Environment,
        )

    return AbilityFactory.WhenThisRevealed(
        None,
        discard_other_enviroment,
    )

