from . import *

# Out for Blood

def GetAbilities() -> Sequence['Ability']:

    def out_for_blood_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        unit = Filter.One(Worlds.GetOnFieldFriendlyCharacters(effect), effect, fewest_remaining_hp=True)
        if unit:
            defeated = False
            def action():
                nonlocal defeated
                defeated = True

            defeat_effects = this.effect.Registers(
                AbilityFactory.WhenUnitBeDefeated(
                    AbilityType.Temp0,
                    None,
                    lambda effect, message:
                        action(),
                    conditions=[
                        lambda defeat_effect, defeat_message:
                            defeat_message.by_effect == effect
                    ],
                )
            )

            this.DealDamage([unit], 1, effect)
            Effects.UnRegister(defeat_effects)
            if defeated:
                player = message.GetToPlayer()
                Faces.ResolveAbility([this], player, AbilityType.WhenRevealed, effect)


    def out_for_blood_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        Faces.ResolveAbility([this], player, AbilityType.WhenRevealed, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            out_for_blood_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            out_for_blood_boost
        ),
    ]

