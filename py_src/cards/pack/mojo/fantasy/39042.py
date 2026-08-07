from . import *

# Dragon

def GetAbilities() -> Sequence['Ability']:

    def dragon_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        def action(player: 'Player'):
            player.DrawUp(4, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.UnitTakeAdditionalDamageFromCards(
            "This",
            "Double",
            CardFinder(has_printed_res="Y"),
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            dragon_defeated
        ),
    ]

