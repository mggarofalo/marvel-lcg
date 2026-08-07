from . import *

# Partnership of Pain

def GetAbilities() -> Sequence['Ability']:

    def partnership_of_pain_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villains = Worlds.GetVillains(effect)

        if villains:
            villain = GetLowestOrder(villains)[0]

            value = 0
            for check_villain in villains:
                if check_villain != villain:
                    value += check_villain.scheme
            player = message.GetToPlayer()
            villain.DoSchemes(player, effect, property=SchemeProperty(additional_value=value))


    def partnership_of_pain_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villains = Worlds.GetVillains(effect)

        if villains:
            villain = GetHighestOrder(villains)[0]

            value = 0
            for check_villain in villains:
                if check_villain != villain:
                    value += check_villain.attack
            player = message.GetToPlayer()
            villain.DoAttackYou(player, effect, property=AttackProperty(additional_value=value))


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            partnership_of_pain_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            partnership_of_pain_hero
        ),
    ]

