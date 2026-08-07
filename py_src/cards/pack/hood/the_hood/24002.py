from . import *

# * The Hood

def GetAbilities() -> Sequence['Ability']:

    def the_hood_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        AsideModularSet.ChooseRandom(effect, do_shuffle=True)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_hood_revealed
        ),
        FoulPlay(2, True),
    ]

