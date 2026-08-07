from . import *

# Sinister Onslaught

def GetAbilities() -> Sequence['Ability']:

    def sinister_onslaught_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        do_scheme = False
        for unit in Worlds.FindCardsOnField(
            effect,
            trait="CRIMINAL",
            card_type=Enemy
        ):
            unit.DoSchemes(player, effect)
            do_scheme = True
        if not do_scheme:
            ThisCardGainSurge(effect)


    def sinister_onslaught_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        do_attack = False
        enemies = Worlds.FindCardsOnField(
            effect,
            trait="CRIMINAL",
            card_type=Enemy
        )
        for unit in enemies:
            unit.DoAttackYou(player, effect)
            do_attack = True
        if not do_attack:
            ThisCardGainSurge(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            sinister_onslaught_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            sinister_onslaught_hero
        ),
    ]

