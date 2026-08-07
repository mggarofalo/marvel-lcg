from . import *

# Shieldmaiden

def GetAbilities() -> Sequence['Ability']:

    def shieldmaiden(effect: 'Effect', message: 'Message.WhenUnitBeingAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        valkyrie = effect.targets[0].CastTo(Hero)
        valkyrie.GainForThisActive(
            effect,
            message.would_atk_message,
            defense=2)
        message.DeclareDefender(valkyrie, effect)

    return [
        AbilityFactory.WhenUnitBeingAttack(
            AbilityType.HeroInterrupt,
            None,
            HAS_DEATH_GLOW_FINDER,
            shieldmaiden,
        ).SetPlay().SetLabel('defense')
        .SetTarget(name="Valkyrie"),
    ]

