from . import *

# All for One

def GetAbilities() -> Sequence['Ability']:

    def all_for_one(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        damage = 3

        def action(targets: Sequence[CardFace]):
            nonlocal damage
            faces = Faces.ExhaustAll(targets, effect)
            damage += len(faces)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                action
            ).SetTarget("YouControlUnit", trait="AVENGER", canbe_exhaust=True,
                range=(0, "All"))
        )

        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            all_for_one
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

