from . import *

# Astral Projection

def GetAbilities() -> Sequence['Ability']:

    def astral_projection(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = 3
        initiator = effect.GetInitiator()

        faces = initiator.LookAtDeck("EncounterDeck", 1, effect)
        value += FacesCounter.CountTotalBoostIcons(faces)

        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            astral_projection
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
    ]

