from . import *

# Hex Bolt

def GetAbilities() -> Sequence['Ability']:

    def hex_bolt(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = Worlds.DiscardEncounterCards(3, effect)
        faces = initiator.ChooseOrder(faces)
        for face in faces:
            boost_icons = FacesCounter.CountTotalBoostIcons([face])
            if boost_icons == 0:
                initiator.ChooseAbilities(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "",
                        lambda targets:
                            this.DealDamage(targets, 2, effect)
                    ).SetTarget(Enemy)
                )
            elif boost_icons == 1:
                initiator.ChooseAbilities(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "",
                        lambda targets:
                            this.RemoveThreatFromSchemes(targets, 2, effect)
                    ).SetTarget(Scheme2)
                )
            elif boost_icons == 2:
                initiator.DrawUp(1, effect)
            elif boost_icons >= 3:
                status = initiator.DeclareStatusCard()
                if status:
                    the_status: CardFace.STATUS = status
                    initiator.ChooseAbilities(
                        effect,
                        AbilityFactory.ForChoiceAbility(
                            "",
                            lambda targets:
                                Faces.GiveStatus(targets, the_status, effect)
                        ).SetTarget(Unit2, canbe_status=the_status)
                    )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            hex_bolt
        ).SetPlay().SetLabel(),
    ]

