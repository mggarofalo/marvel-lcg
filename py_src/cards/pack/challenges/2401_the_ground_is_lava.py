from . import *

# The Ground is Lava

def GetAbilities() -> Sequence['Ability']:

    def the_ground_is_lava(effect: 'Effect', message: 'Message.AfterUnitChangeForm'):
        this = effect.this.CastTo(Challenge)
        Unused(this)

        this.DealDamage([message.trigger], 2, effect)


    return [
        AbilityFactory.UsingOnlyHeroes(
            has_or_can_gain_the_trait="AERIAL"
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Challenge,
            Identity,
            the_ground_is_lava,
            to_form=AlterEgo
        )
    ]

