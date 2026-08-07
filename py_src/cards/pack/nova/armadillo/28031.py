from . import *

# Tough and Tumble

def GetAbilities() -> Sequence['Ability']:

    def tough_and_tumble_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            ThisCardGainSurge(effect)

        Worlds.Enemies.AllEnemySchemeAgainstYou(effect, player, CardFinder(with_tough=True), if_no_activate=action)

    def tough_and_tumble_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        activated = Worlds.Enemies.AllEnemyAttackYou(effect, player, CardFinder(with_tough=True))
        if not activated.activated_cnt:
            ThisCardGainSurge(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            tough_and_tumble_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            tough_and_tumble_hero
        ),
    ]

