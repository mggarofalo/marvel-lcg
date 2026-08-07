from . import *

# Avatar of Death

def GetAbilities() -> Sequence['Ability']:

    def avatar_of_death_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoSchemes(player, effect)

    def avatar_of_death_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(player, effect, property=AttackProperty(overkill=True, piercing=True))


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            avatar_of_death_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            avatar_of_death_hero
        ),
    ]

