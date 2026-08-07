from . import *

# Whose Show is it Anyway?

def GetAbilities() -> Sequence['Ability']:

    def whose_show_is_it_anyway(effect: 'Effect', message: 'Message.WhenCardWouldReveal'):
        player = message.GetToPlayer()
        message.SetBeInstead(effect)
        message.trigger.PutIntoPlay(player, effect)

    return [
        AbilityFactory.UsingOnlyHeroes(
            ["Spider-Ham", "Deadpool"]
        ),
        AbilityFactory.CardCannotLeavePlayWhile(
            AbilityType.Challenge,
            CardFinder2("SHOW", Environment),
        ),
        AbilityFactory.WhenCardWouldReveal(
            AbilityType.Challenge,
            "AnyPlayer",
            CardFinder2("SHOW", Environment),
            whose_show_is_it_anyway
        )
    ]

