from . import *

# * Lucia von Bardas

def GetAbilities() -> Sequence['Ability']:

    def lucia_von_bardas(effect: 'Effect', message: 'Message.AfterPhaseEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = Worlds.FindCardsOnField(
            effect,
            name="Lucia von Bardas",
            card_type=Unit2
        )
        Faces.GiveStatus(faces, "Tough", effect)


    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Minion).tough > 0,
            scheme=1,
            attack=1,
            change_on_event=OnEvent.Status("This", 'Tough'),
        ),
        AbilityFactory.AfterPhaseEnd(
            AbilityType.ForcedResponse,
            "Villain",
            lucia_von_bardas
        ),
    ]

