from . import *

# Klaw's Vengeance

def GetAbilities() -> Sequence['Ability']:

    def klaw_s_vengeance_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        player = message.GetToPlayer()
        player.DiscardRandomHandCards(1, effect)

    def klaw_s_vengeance_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindCardOnField(
            effect,
            name="Klaw",
            card_type=Villain
        )

        if villain:
            villain.DoAttackYou(player, effect,
                if_this_attack_deals_damage=lambda damage:
                    this.PlaceThreatOnSchemes("MainScheme", 1, effect)
            )

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            klaw_s_vengeance_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            klaw_s_vengeance_hero
        )
    ]
