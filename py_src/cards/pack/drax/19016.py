from . import *

# Hard Knocks

def GetAbilities() -> Sequence['Ability']:

    def hard_knocks(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        def give_tough(defeated_effect: 'Effect', defeated_message: 'Message.WhenUnitBeDefeated'):
            Faces.GiveStatus([initiator.GetHero()], "Tough", effect)

        initiator = effect.GetInitiator()
        defeated_effects: List[Effect] = []
        for unit in effect.targets:
            defeated_effects += unit.effect.RegisterTemp(
                AbilityFactory.WhenUnitBeDefeated(
                    AbilityType.Temp0,
                    unit,
                    give_tough,
                ),
                unregister_after_exec=False,
            )

        this.DealDamage(effect.targets, 4, effect)
        Effects.UnRegister(defeated_effects)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            hard_knocks
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

