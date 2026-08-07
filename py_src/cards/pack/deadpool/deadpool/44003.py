from . import *

# Exhausting Personality

def GetAbilities() -> Sequence['Ability']:

    def exhausting_personality(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        scheme = Worlds.FindMainScheme(this)

        if scheme:
            acceleration_token = scheme.acceleration_token
        else:
            acceleration_token = 0

        def stun_and_confuse(targets: Sequence['CardFace']):
            Faces.GiveStatus(targets, "Stunned", effect)
            Faces.GiveStatus(targets, "Confused", effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Place 1 acceleration token on the main scheme → stun and confuse the villain",
                stun_and_confuse
            ).SetTarget(Villain, canbe_status=["Stunned", "Confused"])
            .SetCostFunc(CostFunc.PlaceTokens("MainScheme", 1, 'acceleration_token')),
            AbilityFactory.ForChoiceAbility(
                "Exhaust a player's identity → that player draws 1 card for each acceleration token on the main scheme",
                lambda targets:
                    Players.DrawUp(targets, acceleration_token, effect),
                targets_is_exhaust_cost=True
            ).SetCostFunc(CostFunc.Exhaust("Players"))
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            exhausting_personality
        ).SetPlay().SetLabel()
        .SetTarget2(Villain, canbe_status=["Stunned", "Confused"])
        .SetTarget2(Identity, canbe_exhaust=True)
    ]

