from . import *

# Rage of Ultron

def GetAbilities() -> Sequence['Ability']:

    def rage_of_ultron_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoSchemes(player, effect, for_each_threat_placed=lambda placed_threat:
                player.DiscardDeckTopCards(placed_threat, effect)
            )

    def rage_of_ultron_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(player, effect,
                if_this_attack_deals_damage=lambda dealt_damage:
                    player.DiscardDeckTopCards(dealt_damage, effect)
            )


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            rage_of_ultron_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            rage_of_ultron_hero
        ),
    ]
