from . import *

# Lightning Blast

def GetAbilities() -> Sequence['Ability']:

    def lightning_blast_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        scheme = Worlds.FilterSideScheme(effect, most_threat=True)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 3, effect)

    def lightning_blast_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = GetThunderBall(effect)
        player = message.GetToPlayer()

        def action():
            scheme = villain.FindCorrespondingScheme()
            if scheme:
                this.PlaceThreatOnSchemes([scheme], 3, effect)
        villain.DoAttackYou(player, effect, if_undefended=action)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            lightning_blast_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            lightning_blast_hero
        )
    ]

