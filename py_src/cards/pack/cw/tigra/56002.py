from . import *

# * Moon Knight: Marc Spector

def GetAbilities() -> Sequence['Ability']:

    def moon_knight(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        minion = Search.EncounterCard(
            effect,
            initiator,
            include_discard_pile=True,
            card_type=Minion,
        )
        if minion:
            minion.PutIntoPlay(initiator, effect)
            Faces.GiveStatus([minion], "Stunned", effect)
            Faces.GiveStatus([minion], "Confused", effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.ForcedResponse,
            "This",
            moon_knight
        ),
    ]

