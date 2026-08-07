from . import *

# Weapon Master

def GetAbilities() -> Sequence['Ability']:

    def weapon_master_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        player = message.GetToPlayer()
        if villain:
            villain.DoSchemes(player, effect)
            if villain.GetInventoryDeck().FindCard(trait="WEAPON", card_type=Attachment):
                ThisCardGainSurge(effect)

    def weapon_master_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(player, effect)
            if villain.GetInventoryDeck().FindCard(trait="WEAPON", card_type=Attachment):
                ThisCardGainSurge(effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            weapon_master_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            weapon_master_hero
        ),
    ]

