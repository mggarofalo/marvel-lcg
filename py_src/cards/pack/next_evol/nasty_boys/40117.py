from . import *

# Get Nasty

def GetAbilities() -> Sequence['Ability']:

    def get_nasty_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.GetOnFieldMinions(effect)
        value = len(faces)
        value += len(CardFinder2("NASTY BOY").Checks(faces))
        this.PlaceThreatOnSchemes([this], value, effect)

        minion = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            trait="NASTY BOY",
            card_type=Minion
        )

        if minion:
            minion.Reveal(None, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Minion,
            attack=1,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            get_nasty_revealed
        ),
    ]

