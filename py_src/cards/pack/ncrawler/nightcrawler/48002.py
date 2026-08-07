from . import *

# * Daytripper: Amanda Sefton

def GetAbilities() -> Sequence['Ability']:

    def daytripper(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            name="Bamf!",
            card_type=Upgrade
        )
        if face:
            if not initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        face.AttachTo2(targets[0], effect)
                ).SetTarget(Enemy, canbe_attach_by=face)
            ):
                Faces.DiscardAll([face], effect)

        this.DealDamage(effect.targets2, 1, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            daytripper
        )
        .SetTarget("This")
        .SetTarget2(Enemy, with_attach=CardFinder(name="Bamf!"), range="All"),
    ]

