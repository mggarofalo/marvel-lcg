from . import *

# Psychic Manipulation

def GetAbilities() -> Sequence['Ability']:

    def psychic_manipulation(effect: 'Effect', message: 'Message.WhenUnitWouldScheme') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.SetRemoveThreatInsteadOfPlacing(effect)


    return [
        AbilityFactory.WhenUnitWouldScheme(
            AbilityType.Interrupt,
            Villain,
            psychic_manipulation
        ).SetPlay(only_if_your_identity_has_trait="PSIONIC").SetLabel('thwart'),
    ]

