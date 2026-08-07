from . import *

# * Albert Moon

def GetAbilities() -> Sequence['Ability']:

    def albert_moon(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        value = 0
        silk = Worlds.FindCardOnField(effect, name="Cindy Moon")
        if silk:
            value = silk.GetPlacedCardArea().GetSize()

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Tuck the top card of the encounter deck under Cindy Moon",
                lambda targets:
                    TuckCardUnderCindyMoon(targets, effect)
            ).SetTarget("EncounterDeckTop"),
            AbilityFactory.ForChoiceAbility(
                f"Heal {value} damage from Cindy Moon",
                lambda targets:
                    this.HealthUnits(targets, value, effect)
            ).SetTarget(name="Cindy Moon", canbe_heal=True)
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            albert_moon
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

