from . import *

# Infinite Power

def GetAbilities() -> Sequence['Ability']:

    def infinite_power_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)

            player = message.GetToPlayer()
            villain.DoSchemes(player, effect)


    def infinite_power_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)
            villain.DoAttackYou(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            infinite_power_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            infinite_power_hero
        ),
    ]

