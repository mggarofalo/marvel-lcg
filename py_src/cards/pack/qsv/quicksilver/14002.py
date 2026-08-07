from . import *

# * Scarlet Witch: Wanda Maximoff

def GetAbilities() -> Sequence['Ability']:

    def scarlet_witch(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        faces = Worlds.DiscardEncounterCards(1, effect)
        value = FacesCounter.CountTotalBoostIcons(faces)
        message.GainValue(value, effect)


    return [
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.Interrupt,
            "This",
            scarlet_witch
        )
    ]

