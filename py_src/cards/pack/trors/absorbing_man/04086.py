from . import *

# Swinging Stone

def GetAbilities() -> Sequence['Ability']:

    def swinging_stone_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = GetAbsorbingMan(effect)
        if AbsorbingManHasTrait(effect, 'STONE'):
            additional_value = 1
        else:
            additional_value = 0

        player = message.GetToPlayer()
        villain.DoSchemes(player, effect, property=SchemeProperty(additional_value=additional_value))

    def swinging_stone_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = GetAbsorbingMan(effect)
        if AbsorbingManHasTrait(effect, 'STONE'):
            additional_value = 1
        else:
            additional_value = 0
        villain.DoAttackYou(player, effect, property=AttackProperty(additional_value=additional_value))


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            swinging_stone_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            swinging_stone_hero
        ),
    ]

