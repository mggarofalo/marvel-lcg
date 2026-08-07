from . import *

# Bird of Prey

def GetAbilities() -> Sequence['Ability']:

    def bird_of_prey(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        damage = 4

        def action(targets: Sequence['CardFace']):
            nonlocal damage
            Faces.DiscardAll(targets, effect)
            damage += FacesCounter.CountTotalBoostStarAndBoost(targets)

        initiator.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard the top card of the encounter deck to deal additional damage",
                action
            ).SetTarget("EncounterDeckTop")
        )

        this.DealDamage(effect.targets, damage, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            bird_of_prey
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

