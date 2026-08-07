from . import *

# Photonic Blast

def GetAbilities() -> Sequence['Ability']:

    def photonic_blast_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, ally=True, support=True)

    def photonic_blast_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = FindCaptainMarvel(effect)
        if villain:
            villain.DoAttackYou(player, effect)

        PlaceEnergyCounterOnEnergyChannel(1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            photonic_blast_revealed
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            photonic_blast_hero
        ),
    ]

