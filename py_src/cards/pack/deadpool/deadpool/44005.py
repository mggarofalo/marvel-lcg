from . import *

# Metaknowledge

def GetAbilities() -> Sequence['Ability']:

    def metaknowledge(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        message.CancelAllEffectsAndDiscardIt(effect)

        face = message.trigger
        damage = FacesCounter.CountTotalBoostIcons([face]) + FacesCounter.CountTotalBoostStar([face])
        initiator.GetIdentity().TakeDamage(this, damage, effect)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "AnyPlayer",
            EncounterCard,
            "All",
            metaknowledge,
        ).SetPlay().SetLabel(),
    ]

