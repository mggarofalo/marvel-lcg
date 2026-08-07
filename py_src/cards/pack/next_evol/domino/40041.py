from . import *

# Luck Be a Lady

def GetAbilities() -> Sequence['Ability']:

    def luck_be_a_lady(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.DiscardDeckTopCard(effect)
        res_dict = FacesCounter.GetPrintedResourcesCountInternal([face], initiator.player_deck)
        r = res_dict["R"]
        b = res_dict["B"]
        y = res_dict["Y"]
        g = res_dict["G"]

        ability_y = AbilityFactory.ForChoiceAbility(
            "Heal 2 damage from a character",
            lambda targets:
                this.HealthUnits(targets, 2, effect)
        ).SetTarget(Unit2, canbe_heal=True)
        ability_b = AbilityFactory.ForChoiceAbility(
            "Remove 2 threat from a scheme",
            lambda targets:
                this.RemoveThreatFromSchemes(targets, 2, effect)
        ).SetTarget(Scheme2)
        ability_r = AbilityFactory.ForChoiceAbility(
            "Deal 3 damage to an enemy",
            lambda targets:
                this.DealDamage(targets, 3, effect)
        ).SetTarget(Enemy)

        while y + b + r + g > 0:
            if y:
                initiator.ChooseAbilities(
                    effect,
                    ability_y
                )
                y -= 1
            if b:
                initiator.ChooseAbilities(
                    effect,
                    ability_b
                )
                b -= 1
            if r:
                initiator.ChooseAbilities(
                    effect,
                    ability_r
                )
                r -= 1
            if g:
                initiator.ChooseAbilities(
                    effect,
                    ability_y,
                    ability_b,
                    ability_r,
                )
                g -= 1


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            luck_be_a_lady
        ).SetPlay().SetLabel(),
    ]

