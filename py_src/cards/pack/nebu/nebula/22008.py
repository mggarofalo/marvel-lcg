from . import *

# Wide Stance

def GetAbilities() -> Sequence['Ability']:

    def wide_stance(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.LookAtDeck("EncounterDeck", 3, effect)
        face = initiator.AskDiscardFace(faces, effect, not_shuffle=True)
        if face:
            faces.remove(face)
        initiator.PlaceOnTopInAnyOrder(faces, effect)


    return [
        AbilityFactory.ReduceCharacterTakeDamageBy(
            CardFinder(name="Nebula", card_type=Hero),
            1,
            from_each_attack=True,
        ),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            wide_stance
        ),
    ]

