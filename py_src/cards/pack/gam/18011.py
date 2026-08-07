from . import *

# * Angela: Aldrif Odinsdottir

def GetAbilities() -> Sequence['Ability']:

    def angela(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        engage_ok = False

        initiator = effect.GetInitiator()
        face = Search.EncounterCardTop(
            effect,
            initiator,
            10,
            card_type=Minion,
        )
        if face:
            engage_ok = face.PutIntoPlay(initiator, effect)

        if not engage_ok:
            Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.ForcedResponse,
            "This",
            angela
        ),
    ]

