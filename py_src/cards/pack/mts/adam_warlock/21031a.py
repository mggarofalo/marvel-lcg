from . import *

# * Adam Warlock

def GetAbilities() -> Sequence['Ability']:

    def adam_warlock(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.DiscardAll(effect.targets, effect)
        for face in effect.targets:
            if ClassCard.IsType(face):
                if face.IsClass("Aggression"):
                    initiator.ChooseAbilities(
                        effect,
                        AbilityFactory.ForChoiceAbility(
                            "",
                            lambda targets:
                                this.DealDamage(targets, 2, effect)
                        ).SetTarget(Enemy)
                    )
                if face.IsClass("Justice"):
                    initiator.ChooseAbilities(
                        effect,
                        AbilityFactory.ForChoiceAbility(
                            "",
                            lambda targets:
                                this.RemoveThreatFromSchemes(targets, 2, effect)
                        ).SetTarget(Scheme2)
                    )
                if face.IsClass("Protection"):
                    initiator.ChooseAbilities(
                        effect,
                        AbilityFactory.ForChoiceAbility(
                            "",
                            lambda targets:
                                this.HealthUnits(targets, 1, effect)
                        ).SetTarget(Ally, canbe_heal=True)
                    )
                if face.IsClass("Leadership"):
                    def action(targets: Sequence[CardFace]):
                        for target in targets:
                            if Hero.IsType(target):
                                target.GainUntilRoundEnd(
                                    effect,
                                    thwart=1,
                                    attack=1,
                                    defense=1
                                )
                    initiator.ChooseAbilities(
                        effect,
                        AbilityFactory.ForChoiceAbility(
                            "",
                            action
                        ).SetTarget(Hero)
                    )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            adam_warlock
        ).SetName("Battle Mage")
        .SetTarget(from_where=["YourHandCards"])
        .LimitOncePerPhase(),
    ]

