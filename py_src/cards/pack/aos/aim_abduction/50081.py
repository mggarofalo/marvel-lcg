from . import *

# Abduct Superhumans

def GetAbilities() -> Sequence['Ability']:

    def abduct_superhumans_forced(effect: 'Effect', message: 'Message.WhenCardLeavePlay') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        ally = message.trigger.CastTo(Ally)
        value = ally.printed_cost.val
        this.TuckCardUnderHere(ally, effect)
        this.PlaceThreatOnSchemes([this], value, effect)
        this.PlaceAccelerationToken(1, effect)


    def abduct_superhumans(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = this.GetPlacedCardArea().Get()
        for face in faces:
            player = face.GetOwnerPlayer()
            face.PutIntoPlay(player, effect, under_control=True)


    return [
        AbilityFactory.WhenCardLeavePlay(
            AbilityType.ForcedInterrupt,
            Ally,
            abduct_superhumans_forced,
            conditions=[
                lambda effect, message:
                    not message.into_area.flags.is_place_card_area
            ]
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            abduct_superhumans,
        ),
    ]

