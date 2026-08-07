from . import *

# Mind Ray

def GetAbilities() -> Sequence['Ability']:

    def mind_ray_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        zola = Worlds.FindVillain(effect)
        if zola:
            zola.DoSchemes(player, effect)

        Faces.GiveStatus([player.GetIdentity()], "Confused", effect)

    def mind_ray_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        hero = player.GetHero()

        zola = Worlds.FindVillain(effect)
        if zola:
            zola.DoAttackYou(player, effect)

        Faces.GiveStatus([hero], "Stunned", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            mind_ray_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            mind_ray_hero
        )
    ]

