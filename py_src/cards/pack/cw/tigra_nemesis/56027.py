from . import *

# * Madame Masque

def GetAbilities() -> Sequence['Ability']:

    def madame_masque_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.FindCardOnField(effect, name="Criminal Underworld", card_type=EncounterSideScheme)
        if face:
            this.PlaceThreatOnSchemes([face], 2, effect)
        else:
            Find.FindAndReveal(effect, player, name="Criminal Underworld")

    def madame_masque_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Worlds.FindCardOnField(effect, name="Criminal Underworld", card_type=EncounterSideScheme)
        if face:
            this.PlaceThreatOnSchemes([face], 2, effect)
        else:
            message.GainBoostIcon(2, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            madame_masque_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            madame_masque_boost
        ),
    ]

