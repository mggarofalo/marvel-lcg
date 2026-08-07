from . import *

# Night Vision Goggles

def GetAbilities() -> Sequence['Ability']:

    def night_vision_goggles(effect: 'Effect', message: 'Message.WhenResolvePreparationAbility') -> None:
        this = effect.this
        Unused(this)

        message.PreventAllDamage(effect)

        face = Worlds.FindCardOnField(effect, name="Night Vision Goggles")
        if face:
            Faces.DiscardAll([face], effect)

    def night_vision_goggles_preparation(effect: 'Effect', message: 'Message.WhenResolvePreparationAbility') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        villain = Worlds.FindCardOnField(effect, name="Black Widow")
        if villain:
            this.AttachTo2(villain, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Black Widow")
        ),
        AbilityFactory.GiveAbilityToInPlayWhenApplyThis(
            EncounterCard,
            get_new_value=lambda effect, face: 0 if face.ability.Find(type=AbilityType.Preparation) else 1,
            abilities=[AbilityFactory.WhenResolvePreparationAbility(
                "This",
                night_vision_goggles
            )],
        ),
        AbilityFactory.WhenResolvePreparationAbility(
            "This",
            night_vision_goggles_preparation
        ),
    ]

