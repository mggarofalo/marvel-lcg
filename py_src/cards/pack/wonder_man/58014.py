from . import *

# * Scarlet Witch: Wanda Maximoff

def GetAbilities() -> Sequence['Ability']:

    def scarlet_witch(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Worlds.DiscardEncounterTopCard(effect)
        if Treachery.IsType(face):
            face.ResolveAbility(initiator, AbilityType.WhenRevealed, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.ForcedResponse,
            "You",
            "This",
            scarlet_witch
        ),
    ]

