from . import *

# * Boom Boom: Tabitha Smith

def GetAbilities() -> Sequence['Ability']:

    def boom_boom(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.PlaceCountersOn([message.attacked], 1, 'bomb', effect)

        def action(effect: 'Effect', message: 'Message.WhenPhaseEnd') -> None:
            faces = Worlds.FindCardsOnField(
                effect,
                card_type=CanPlaceCounter,
                has_counter='bomb',
            )

            counters = Faces.RemoveCountersOn(faces, "All", 'bomb', effect)

            if counters:
                damage = 2 * counters
                targets = Worlds.GetOnFieldEnemies(effect)
                this.DealDamage(targets, damage, effect)

        this.effect.RegisterTemp(
            AbilityFactory.WhenPlayerPhaseEnd(
                AbilityType.Temp0,
                action,
            ),
            unregister_after_exec=True,
            until_phase_end=True
        )

    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.Response,
            "This",
            Enemy,
            boom_boom,
        )
    ]

