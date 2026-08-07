from . import *

# "Think Fast!"

def GetAbilities() -> Sequence['Ability']:

    def think_fast(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GetIdentity().TakeDamage(this, 1, effect)
        Faces.GiveStatus(effect.targets, "Confused", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            think_fast,
        ).SetPlay(only_if_your_identity_has_trait="GUARDIAN").SetLabel()
        .SetTarget(Villain)
    ]

