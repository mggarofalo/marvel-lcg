from . import *

# Defy Danger

def GetAbilities() -> Sequence['Ability']:

    def defy_danger(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 5, effect)

        face = Worlds.DiscardEncounterTopCard(effect)
        if face:
            value = FacesCounter.CountTotalBoostIcons([face])

            initiator = effect.GetInitiator()
            initiator.GetIdentity().TakeDamage(this, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            defy_danger
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

