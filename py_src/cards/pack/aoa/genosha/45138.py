from . import *

# Police State

def GetAbilities() -> Sequence['Ability']:

    def police_state(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        Find.FindAndReveal(
            effect,
            player,
            name="Escaped Mutant",
            card_type=Attachment,
        )


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            police_state,
            has_defeating_player=True
        ),
    ]

