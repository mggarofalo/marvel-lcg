from . import *

# * Griffin

def GetAbilities() -> Sequence['Ability']:

    def griffin(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.GiveStatus([message.attacked], "Stunned", effect)


    def griffin_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        encounter_deck = Worlds.GetEncounterDeck(effect)
        if [x for x in Worlds.GetOnFieldFriendlyCharacters(effect) if x.IsStunned()] != []:
            Faces.ShuffleAllTo([this], encounter_deck, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            None,
            griffin,
            target_in_play=True,
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            griffin_defeated
        ),
    ]

