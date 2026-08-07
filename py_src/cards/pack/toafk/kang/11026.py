from . import *

# Energy Blast

def GetAbilities() -> Sequence['Ability']:

    def energy_blast_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        if not player.DiscardControlCards(effect, ally=True, support=True):
            ThisCardGainSurge(effect)

    def energy_blast_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindCardOnField(
            effect,
            name="Kang",
            card_type=Villain
        )
        if villain:
            villain.DoAttackYou(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            energy_blast_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            energy_blast_hero
        )
    ]

