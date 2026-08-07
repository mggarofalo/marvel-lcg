from . import *

# Thoughtcasting

def GetAbilities() -> Sequence['Ability']:

    def thoughtcasting_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.DiscardHandCards((1, 1), effect, highest_cost=True)

        if faces:
            value = FacesCounter.GetPrintedCost(faces)
            main_scheme = Worlds.FindMainScheme(this)
            if main_scheme:
                this.PlaceThreatOnSchemes([main_scheme], value, effect)


    def thoughtcasting_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.DiscardHandCards((1, 1), effect, highest_cost=True)

        if faces:
            hero = player.GetHero()
            value = FacesCounter.GetPrintedCost(faces)
            hero.TakeDamage(this, value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Hero",
            thoughtcasting_hero
        ),
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            thoughtcasting_alter_ego
        ),
    ]

