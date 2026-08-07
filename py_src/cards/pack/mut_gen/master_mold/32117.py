from . import *

# Master Mold's Children

def GetAbilities() -> Sequence['Ability']:

    def master_molds_children_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        if not player.GetEngagedMinions():
            master_mold = Worlds.FindCardOnField(
                effect,
                card_type=Enemy,
                name="Master Mold"
            )
            if master_mold:
                master_mold.DoSchemes(player, effect)
        else:
            Worlds.Enemies.EngagedMinionSchemes(effect, player)

    def master_molds_children_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        if not player.GetEngagedMinions():
            master_mold = Worlds.FindCardOnField(
                effect,
                card_type=Enemy,
                name="Master Mold"
            )
            if master_mold:
                master_mold.DoAttackYou(player, effect)
        else:
            Worlds.Enemies.EngagedMinionAttackYou(effect, player)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            master_molds_children_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            master_molds_children_hero
        ),
    ]

