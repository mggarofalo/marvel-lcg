from . import *

# Stealth

def GetAbilities() -> Sequence['Ability']:

    def stealth(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        if this.GetTokens('threat') <= 5:
            def action(message: 'Message.WhenUnitWouldScheme'):
                def place_here(effect: 'Effect', message: 'Message.WhenSchemeWouldPlaceThreat'):
                    message.UpdateValue(-1, effect)
                    Faces.PlaceTokensOn([this], 1, 'threat', effect)
                this.effect.RegisterTemp(
                    AbilityFactory.WhenSchemeWouldPlaceThreat(
                        AbilityType.Temp0,
                        None,
                        place_here,
                        by_scheme=message
                    ),
                    unregister_after_exec=True,
                    until_event_end=message
                )
        else:
            def action(message: 'Message.WhenUnitWouldScheme'):
                pass
        message.DoSchemeInstead(effect, operation=action)


    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterruptHero,
            Enemy,
            stealth
        ),
    ]

