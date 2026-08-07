from . import *

# Festering Mass

def GetAbilities() -> Sequence['Ability']:

    def festering_mass_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        this.Reveal(player, effect)

    def festering_mass(effect: 'Effect', ui: List['CardFace']):
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        found_faces = Worlds.FindCardsOnField(
            effect,
            card_type=Environment,
            trait="SYMBIOTE"
        )
        if found_faces == [] or len(found_faces) == 1 and found_faces[0] == effect.this:
            ui.extend(found_faces)
            return 1
        elif len(found_faces) == 1 and found_faces[0].card.state.is_leaving_play:
            ui.extend(found_faces)
            return 1
        return 0

    return [
        AbilityFactory.ThisGainKeyword(
            festering_mass,
            consider_as=("SYMBIOTE", Environment),
            change_on_event=OnEvent.CardInPlay(Environment)
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            festering_mass_boost
        )
    ]

