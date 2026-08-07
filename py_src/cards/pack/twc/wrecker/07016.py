from . import *

# You're Dead Meat!

def GetAbilities() -> Sequence['Ability']:

    def youre_dead_meat(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = Worlds.GetFirstPlayer(effect)

        def action(targets: Sequence[CardFace]):
            def when_defeated(unit: 'Unit2'):
                scheme = GetWrecker(effect).FindCorrespondingScheme()
                if scheme:
                    this.PlaceThreatOnSchemes([scheme], 3, effect)
            this.DealDamage(targets, 1, effect, if_this_attack_defeats=when_defeated)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                action
            ).SetTarget("Hero|Ally", fewest_remaining_hp=True)
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            youre_dead_meat
        )
    ]

