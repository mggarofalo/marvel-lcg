from . import *

# The Age of Apocalypse

def GetAbilities() -> Sequence['Ability']:

    def apocalypses_printed_hit_point(effect: 'Effect', face: 'CardFace') -> int:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Apocalypse",
            card_type=Unit2
        )
        if villain:
            return villain.printed_health_numeral * Worlds.GetPlayerNumIcon(effect)
        else:
            return 0

    def the_age_of_apocalypse(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        message.SetBeInstead(effect)

        villain = message.trigger
        Faces.DiscardAll(villain.GetAttachedAttachments(), effect)
        this.HealthUnits([villain], "All", effect)

        value = villain.printed_health_numeral

        this.RemoveThreatFromSchemes([this], value, effect, ignore_crisis=True)

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "This",
            get_new_value=apocalypses_printed_hit_point,
            target_threat=1,
            change_on_event=OnEvent.EnterPlayAfter(Villain),
        ),
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Apocalypse"),
            the_age_of_apocalypse,
        ),
    ]

