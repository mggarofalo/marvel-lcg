from . import *

# Pummel

def GetAbilities() -> Sequence['Ability']:

    def pummel_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = GetPiledriver(effect)
        value = 0
        if villain.IsTough():
            value = 2
        player = message.GetToPlayer()
        villain.DoSchemes(player, effect, property=SchemeProperty(additional_value=value))

    def pummel_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = GetPiledriver(effect)

        value = 0
        if villain.IsTough():
            value = 2
        villain.DoAttackYou(player, effect, property=AttackProperty(additional_value=value))

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            pummel_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            pummel_hero
        )
    ]

