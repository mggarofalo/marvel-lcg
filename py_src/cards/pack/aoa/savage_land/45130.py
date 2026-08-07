from . import *

# Giant Ape

def GetAbilities() -> Sequence['Ability']:

    def giant_ape(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetKillerPlayer()
        ResolveSpecialAbilityOnTheSETTINGEnvironment(player, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            giant_ape
        ),
    ]

