from . import *

# * Nick Fury, Sr.

def GetAbilities() -> Sequence['Ability']:

    def nick_fury_sr(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Remove 3 threat from a scheme",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, 3, effect)
            ).SetTarget(Scheme2),
            AbilityFactory.ForChoiceAbility(
                "Draw 2 cards",
                lambda targets:
                    initiator.DrawUp(2, effect)
            ).SetTarget("YourIdentity"),
            AbilityFactory.ForChoiceAbility(
                "Give a [[S.H.I.E.L.D.]] character a tough status card",
                lambda targets:
                    Faces.GiveStatus(targets, "Tough", effect)
            ).SetTarget(Unit2, trait="S.H.I.E.L.D", canbe_tough=True)
        )


    return [
        AbilityFactory.DiscardThisWhenRoundEnd(
            AbilityType.ForcedResponse,
        ),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.ForcedResponse,
            "This",
            nick_fury_sr
        ),
    ]

