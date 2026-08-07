from . import *

# Might Makes Right

def GetAbilities() -> Sequence['Ability']:

    def might_makes_right_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        PlaceSecretCounterOnBoardMemberCard(Environment, 2, effect, with_fewest_secret=True)

    def might_makes_right_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        enemy = Worlds.FindCardOnField(
            effect,
            card_type=Enemy,
            name="Baron Zemo"
        )
        if enemy:

            def action():
                RemoveSecretCountersFromAmongBoardMemberEnvironments(3, effect)

            enemy.DoAttackYou(player, effect, if_no_character_takes_damage=action)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            might_makes_right_revealed
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            might_makes_right_hero
        ),
    ]

