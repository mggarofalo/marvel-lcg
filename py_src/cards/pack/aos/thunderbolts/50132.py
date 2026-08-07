from . import *

# * Citizen V's Sword

def GetAbilities() -> Sequence['Ability']:

    def citizen_vs_sword_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        enemies = Worlds.GetOnFieldEnemies(effect, CardFinder(name="Citizen V"))
        enemy = Filter.One(enemies, effect)

        if enemy:
            player = message.GetToPlayer()
            this.AttachTo2(enemy, effect)
            enemy.DoActivate(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            citizen_vs_sword_revealed
        ),
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.HeroResponse,
            Identity,
            CardFinder(name="Citizen V"),
            DiscardThisCard
        ).SetCost(Cost("RR")),
    ]

