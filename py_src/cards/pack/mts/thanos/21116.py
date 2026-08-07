from . import *

# Sanctuary

def GetAbilities() -> Sequence['Ability']:

    def sanctuary(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.MayChooseOneAbility(
                effect,
                AbilityFactory.ForChoiceAbilityWithCost(
                    Cost("RRR", up_to=True, from_hand=True),
                    "Spend up to 3 [R] resources from their hand",
                    lambda targets, res:
                        # ignore tough
                        this.DealDamage(targets, DamageProperty(res.val * 2, ignore_tough=True), effect)
                ).SetTarget(Villain, name="Thanos")
            )
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            CardFinder(name="Thanos"),
            damage_from=PlayerCard
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            sanctuary,
        ),
    ]

