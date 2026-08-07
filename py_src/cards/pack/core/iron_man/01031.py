from . import *

# Repulsor Blast

def GetAbilities() -> Sequence['Ability']:

    def repulsor_blast(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        initiator = effect.GetInitiator()
        damage = 1

        faces = initiator.DiscardDeckTopCards(5, effect)
        for face in faces:
            res = FacesCounter.GetPrintedResources([face])
            damage += 2 * res.y

        this.DealDamage(effect.targets, damage, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            repulsor_blast
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

