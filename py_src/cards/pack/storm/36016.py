from . import *

# * Gentle: Nezhno Abidemi

def GetAbilities() -> Sequence['Ability']:

    def gentle(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.GainForThisActive(effect, message, attack_consequential_damage=+1)


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.NonKeywordStar,
            "This",
            gentle,
            against_who=Villain
        ),
    ]

