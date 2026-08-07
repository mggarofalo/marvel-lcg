from . import *

# Fearless Determination

def GetAbilities() -> Sequence['Ability']:

    def fearless_determination_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        shield = Find.Find(
            effect,
            name="Cap's Shield",
            card_type=Attachment,
        )
        if shield:
            cap = Worlds.FindCardOnField(effect, name="Captain America", card_type=Leader)
            if cap:
                shield.AttachTo2(cap, effect)
                cap.DoActivate(player, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            fearless_determination_defeated,
        ),
    ]

