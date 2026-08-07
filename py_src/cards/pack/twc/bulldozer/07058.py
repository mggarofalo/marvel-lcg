from . import *

# Headbutt

def GetAbilities() -> Sequence['Ability']:

    def headbutt(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        faces = player.DiscardRandomHandCards(1, effect)
        cost = FacesCounter.GetPrintedCost(faces)

        if player.IsHero():
            player.GetHero().TakeDamage(this, cost, effect)
        else:
            villain = GetBulldozer(effect)
            scheme = villain.FindCorrespondingScheme()
            if scheme:
                this.PlaceThreatOnSchemes([scheme], cost, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            headbutt
        )
    ]

