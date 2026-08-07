from . import *

# Swinging Assault

def GetAbilities() -> Sequence['Ability']:

    def swinging_assault_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.GetAlterEgo().ChangeToForm(Hero, effect)

        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(player, effect)

    def swinging_assault_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(player, effect, property=AttackProperty(additional_boost_card=1))

    return [
        # TODO: Clean this
        # This order is important, ortherwise when we flip into her in alterego form, villain will attack us again
        AbilityFactory.WhenThisRevealed(
            "Hero",
            swinging_assault_hero
        ),
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            swinging_assault_alter_ego
        )
    ]

