from . import *

# * Loki

def GetAbilities() -> Sequence['Ability']:

    def loki(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Worlds.DiscardEncounterTopCard(effect)
        if face and Treachery.IsType(face):
            message.SetBeInstead(effect)
            this.HealHealth("All", effect)


    return [
        # Loki’s Interrupt should be a “Forced Interrupt.” Future reprints will have that errata. Good catch.
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.ForcedInterrupt,
            "This",
            loki
        ),
    ]

