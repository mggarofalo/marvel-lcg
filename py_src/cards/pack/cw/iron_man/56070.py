from . import *

# Repulsor Blast

def GetAbilities() -> Sequence['Ability']:

    def repulsor_blast_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, upgrade=True, support=True)

    def repulsor_blast_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        faces = player.DiscardDeckTopCards(4, effect)
        res = FacesCounter.GetPrintedResources(faces)
        damage = res.y + res.g
        this.DealDamage([identity], damage, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            repulsor_blast_revealed
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            repulsor_blast_hero
        ),
    ]

