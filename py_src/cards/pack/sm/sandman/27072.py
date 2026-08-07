from . import *

# Sand Smash

def GetAbilities() -> Sequence['Ability']:

    def sand_smash_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        ResolveSurgingSandsOnCityStreets(effect)
        ThisCardGainSurge(effect)

    def sand_smash_revealed_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(player, effect, property=AttackProperty(additional_value=1))


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            sand_smash_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            sand_smash_revealed_hero
        ),
    ]

