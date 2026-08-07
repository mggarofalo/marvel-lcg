from . import *

# Magic Barrier

def GetAbilities() -> Sequence['Ability']:

    def magic_barrier(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        message.PreventDamage(3, effect)

        face = initiator.player_deck.GetTop()
        if face and message.attacker:
            res = FacesCounter.GetPrintedResources([face])
            if res.HasColor("Y"):
                this.DealDamage([message.attacker], 3, effect)

    return [
        AbilityFactory.WhenUnitInitiatesAttackAgainst(
            AbilityType.HeroInterrupt,
            Enemy,
            "AnyPlayer",
            magic_barrier
        ).SetPlay().SetLabel('defense'),
    ]

