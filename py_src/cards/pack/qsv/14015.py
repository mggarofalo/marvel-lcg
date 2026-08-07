from . import *

# Side Step

def GetAbilities() -> Sequence['Ability']:

    def side_step(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        message.PreventDamage(3, effect)
        if effect.GetPaidResources().HasColor("Y"):
            units = Worlds.GetOnFieldEnemies(effect)
            unit = initiator.AskChooseFace(units, effect)
            if unit:
                this.DealDamage([unit], 1, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            "You",
            side_step
        ).SetPlay().SetLabel('defense')
    ]

