from . import *

# Hostage Situation

def GetAbilities() -> Sequence['Ability']:

    def hostage_situation_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        allies = Worlds.FindCardsOnField(effect, card_type=Ally, trait="RESCUED")
        ally = Filter.One(allies, effect)
        if ally:
            ally.AttachTo2(this, effect)

    def hostage_situation(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        faces = this.GetInventoryDeck().GetAll()
        for face in faces:
            face.PutIntoPlay(player, effect, under_control=True)


    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            MODOK_FINDER
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            hostage_situation_revealed,
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            hostage_situation,
            has_defeating_player=True
        ),
    ]

