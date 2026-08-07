from . import *

# The Accusation

def GetAbilities() -> Sequence['Ability']:

    def the_accusation_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = Worlds.GetFirstPlayer(effect)

        means = player.AskChoosePaper(["50185", "50186", "50187"])
        motive = player.AskChoosePaper(["50188", "50189", "50190"])
        opportunity = player.AskChoosePaper(["50191", "50192", "50193"])
        units = Worlds.FindCardsOnField(effect, names=["Chief Medical Officer", "Chief Surveillance Officer", "Chief Tactical Officer"])
        accused = player.AskChooseFace(units, effect)
        assert accused

        buff = this.GetBuff(Buff_50168a)
        buff.means = means
        buff.motive = motive
        buff.opportunity = opportunity
        buff.accused = accused

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_accusation_revealed
        ),
    ]

