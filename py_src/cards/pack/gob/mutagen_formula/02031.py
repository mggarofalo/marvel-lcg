from . import *

# Overconfidence

def GetAbilities() -> Sequence['Ability']:

    def overconfidence_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Green Goblin",
            card_type=Villain
        )
        if villain:
            player = message.GetToPlayer()
            def action(placed_threat: int):
                if placed_threat >= 3:
                    this.GainSurge(1, effect)
            villain.DoSchemes(player, effect, for_each_threat_placed=action)

    def overconfidence_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        face = Worlds.FindCardOnField(
            effect,
            name="Green Goblin",
            card_type=Villain
        )
        assert face, f"{effect=}"
        def action(damage: int):
            if damage >= 3:
                this.GainSurge(1, effect)
        face.DoAttackYou(player, effect,
            if_this_attack_deals_damage=action
        )


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            overconfidence_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            overconfidence_hero
        ),
    ]

