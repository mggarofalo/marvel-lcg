from . import *

# Godlike Stamina

def GetAbilities() -> Sequence['Ability']:

    def godlike_stamina(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        for target in effect.targets:
            this.HealthUnits([target], 2, effect)
            initiator = effect.GetInitiator()
            status = target.components.status.GetDeck().Get()
            status_face = initiator.MayChooseFace(status, effect)
            if status_face:
                Faces.DiscardAll([status_face], effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            godlike_stamina,
        ).SetPlay(only_if_your_identity_has_trait="ASGARD").SetLabel()
        .SetTarget("YourIdentity", canbe_heal=True)
    ]

