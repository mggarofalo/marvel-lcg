from . import *

# Death from Above

def GetAbilities() -> Sequence['Ability']:

    def death_from_above_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Green Goblin",
            card_type=Villain
        )
        if villain:
            player = message.GetToPlayer()
            villain.DoSchemes(player, effect, property=SchemeProperty(additional_value=villain.printed_stage))

    def death_from_above_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindCardOnField(
            effect,
            name="Green Goblin",
            card_type=Villain
        )
        if villain:
            villain.DoAttackYou(player, effect, property=AttackProperty(additional_value=villain.printed_stage))

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            death_from_above_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            death_from_above_hero
        ),
    ]

