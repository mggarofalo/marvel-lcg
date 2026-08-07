from . import *

# Combat Ready

def GetAbilities() -> Sequence['Ability']:

    def combat_ready_reveal_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            def action(value: int):
                ship = FindNebulaShip(effect)
                if ship:
                    Faces.PlaceCountersOn([ship], 1, 'evasion', effect)
            villain.DoSchemes(player, effect, for_each_threat_placed=action)

    def combat_ready_reveal_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            def action(damage: int):
                ship = FindNebulaShip(effect)
                if ship:
                    Faces.PlaceCountersOn([ship], 1, 'evasion', effect)
            villain.DoAttackYou(player, effect, if_this_attack_deals_damage=action)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            combat_ready_reveal_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            combat_ready_reveal_hero
        ),
    ]

