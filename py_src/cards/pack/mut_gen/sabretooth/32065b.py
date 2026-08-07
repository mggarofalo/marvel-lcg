from . import *

# Protect the Senator

def GetAbilities() -> Sequence['Ability']:

    def protect_the_senator(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        *RobertKellyCannotBeHealedByPlayerCardEffectsAndCannotHaveUpgradesAttached(),
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.HeroResponse,
            Hero,
            protect_the_senator,
            attacker=CardFinder(name="Sabretooth"),
            conditions=[
                lambda effect, message:
                    # message.defender != None and \
                    message.defender.GetControlByPlayer().IsControl(ROBERT_KELLY_FINDER)
            ],
        ).SetCost(Cost("2"))
        .SetTarget("YourHero", canbe_ready=True),
    ]

