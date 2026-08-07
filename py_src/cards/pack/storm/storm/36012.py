from . import *

# Flash Freeze

def GetAbilities() -> Sequence['Ability']:

    def flash_freeze(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        def get_attack(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
            message.trigger.CastTo(Unit2).GainForThisActive(effect, message, attack=-3)

        get_attack(effect, message)

        initiator = effect.GetInitiator()
        this.effect.RegisterTemp(
            AbilityFactory.WhenUnitAttackYou(
                AbilityType.Temp0,
                initiator.GetEngagedMinions() + Worlds.GetVillains(effect),
                get_attack,
            ),
            unregister_after_exec=False,
            until_phase_end=True
        )

        blizzard = Worlds.FindCardOnField(
            effect,
            name="Blizzard",
            card_type=Support
        )
        if blizzard:
            blizzard.ResolveSpecialAbility(initiator, effect)


    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.HeroInterrupt,
            Villain,
            flash_freeze,
        ).SetPlay().SetLabel('defense')
        .SetTarget("Attacker"),
    ]

