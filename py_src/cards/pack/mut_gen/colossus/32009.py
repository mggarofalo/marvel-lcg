from . import *

# Bulletproof Protector

def GetAbilities() -> Sequence['Ability']:

    def bulletproof_protector(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        def gain_2_tough():
            identity = initiator.GetIdentity()
            Faces.GiveStatus([identity], "Tough", effect)
            Faces.GiveStatus([identity], "Tough", effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Give your hero 2 tough status cards",
                lambda targets:
                    gain_2_tough()
            ),
            AbilityFactory.ForChoiceAbility(
                "Ready your hero",
                lambda targets:
                    Faces.ReadyAll(targets, effect)
            ).SetTarget("YourIdentity", canbe_ready=True)
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            bulletproof_protector
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Discard("YourHeroTough")),
    ]

