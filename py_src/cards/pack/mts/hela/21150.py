from . import *

# The Queen of Hel

def GetAbilities() -> Sequence['Ability']:

    def the_queen_of_hel_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        hela = Worlds.FindVillain(effect)
        if hela:
            hela.DoSchemes(player, effect)
        this.PlaceThreatOnSchemes(Worlds.GetSideSchemes(effect), 1, effect)


    def the_queen_of_hel_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player = message.GetToPlayer()
        hela = Worlds.FindVillain(effect)
        if hela:
            hela.DoAttackYou(player, effect)
        this.PlaceThreatOnSchemes(Worlds.GetSideSchemes(effect), 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            the_queen_of_hel_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            the_queen_of_hel_hero
        ),
    ]

