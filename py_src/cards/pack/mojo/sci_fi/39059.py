from . import *

# ICE-Teroid M

def GetAbilities() -> Sequence['Ability']:

    def ice_teroid_m(effect: 'Effect', message: 'Message.AfterRoundEnd') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        face = Search.EncounterCard(
            effect,
            first_player,
            include_discard_pile=True,
            card_type=Minion
        )
        if face:
            face.PutIntoPlay(first_player, effect)

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Minion,
            guard=1,
            patrol=1,
        ),
        AbilityFactory.AfterRoundEnd(
            AbilityType.ForcedInterrupt,
            ice_teroid_m
        ),
    ]

