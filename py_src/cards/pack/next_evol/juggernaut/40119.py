from . import *

# * Juggernaut

def GetAbilities() -> Sequence['Ability']:

    def juggernaut_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'momentum', effect)

        face = Worlds.FindCardOnField(
            effect,
            name="Juggernaut Exposed",
            card_type=Attachment
        )
        if face:
            face.card.Flip(effect)
        else:
            Faces.GiveStatus([this], "Tough", effect)

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(EncounterVillain).GetCounters('momentum'),
            attack=1,
            change_on_event=OnEvent.Counter("This", 'momentum')
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            juggernaut_revealed
        ),
    ]

