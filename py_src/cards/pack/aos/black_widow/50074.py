from . import *

# Automated Defenses

def GetAbilities() -> Sequence['Ability']:

    def automated_defenses(effect: 'Effect', message: 'Message.WhenResolvePreparationAbility') -> None:
        this = effect.this #.CastTo(SideScheme), cannot add this
        Unused(this)

        if message.attacker:
            this.DealDamage([message.attacker], 1, effect)


    return [
        AbilityFactory.GiveAbilityToInPlayWhenApplyThis(
            EncounterCard,
            get_new_value=lambda effect, face: 0 if face.ability.Find(type=AbilityType.Preparation) else 1,
            abilities=[AbilityFactory.WhenResolvePreparationAbility(
                "This",
                automated_defenses
            )],
        ),
    ]

