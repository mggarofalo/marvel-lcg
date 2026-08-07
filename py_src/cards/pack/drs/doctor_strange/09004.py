from . import *

# Magic Blast

def GetAbilities() -> Sequence['Ability']:

    def magic_blast(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 5, effect)
        initiator = effect.GetInitiator()
        face = initiator.DiscardDeckTopCard(effect)

        res = FacesCounter.GetPrintedResources([face])
        if res.r:
            Faces.GiveStatus(effect.targets, "Stunned", effect)
        if res.y:
            this.DealDamage(effect.targets, 2, effect)
        if res.b:
            Faces.GiveStatus(effect.targets, "Confused", effect)
        if res.g:
            Faces.GiveStatus(effect.targets, "Stunned", effect)
            this.DealDamage(effect.targets, 2, effect)
            Faces.GiveStatus(effect.targets, "Confused", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            magic_blast
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

