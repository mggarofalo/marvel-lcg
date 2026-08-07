from . import *

# A.I.M. Soldier

def GetAbilities() -> Sequence['Ability']:

    def aim_soldier_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            this.PlaceThreatOnSchemes("MainScheme", 1, effect)

        Find.FindAndPutIntoPlay(
            effect,
            player,
            name="A.I.M. Scientist",
            if_not_enter_play=action
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            aim_soldier_revealed
        ),
    ]

