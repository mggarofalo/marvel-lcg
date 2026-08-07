from . import *

# Crime Pays

def GetAbilities() -> Sequence['Ability']:

    def crime_pays_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        face = Search.EncounterCard(
            effect,
            include_discard_pile=False,
            card_type=Minion,
            trait="CRIMINAL",
        )
        do_put = False
        if face:
            if face.PutIntoPlay(player, effect):
                do_put = True
        if not do_put:
            ThisCardGainSurge(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            crime_pays_revealed
        ),
    ]

