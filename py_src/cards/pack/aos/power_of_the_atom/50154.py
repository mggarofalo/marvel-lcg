from . import *

# Runaway Nuclear Reaction

def GetAbilities() -> Sequence['Ability']:

    def runaway_nuclear_reaction(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = message.be_dealt_damage
        this.PlaceThreatOnSchemes([this], value, effect)

        if this.threat >= 10:
            units = Worlds.GetOnFieldCharacters(effect)
            this.DealDamage(units, 10, effect)
            Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AfterFaceBeDealtDamage(
            AbilityType.ForcedResponse,
            CardFinder(name="Radioactive Man"),
            runaway_nuclear_reaction,
        ),
    ]

