from . import *

# Unrelenting Savage

def GetAbilities() -> Sequence['Ability']:

    def unrelenting_savage_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindCardOnField(
            effect,
            name="Sabretooth",
            card_type=Villain
        )
        if villain:
            if villain.sustained == 0:
                value = 1
            else:
                value = 0
            villain.DoSchemes(player, effect, property=SchemeProperty(additional_value=value))

    def unrelenting_savage_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindCardOnField(
            effect,
            name="Sabretooth",
            card_type=Villain
        )
        if villain:
            if villain.sustained == 0:
                value = 1
            else:
                value = 0
            villain.DoAttackYou(player, effect, property=AttackProperty(additional_value=value))

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            unrelenting_savage_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            unrelenting_savage_hero
        ),
    ]

