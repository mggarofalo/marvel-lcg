from . import *

# "I Got This"

def GetAbilities() -> Sequence['Ability']:

    def i_got_this(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        this.DealDamage(effect.targets, 10, effect)

        if Worlds.GetCrisisIcons(effect) > 0:
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.DealDamage(targets, 3, effect)
                ).SetTarget(Enemy)
            )
        if Worlds.GetAccelerationIcons(effect) > 0:
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, 2, effect)
                ).SetTarget(Scheme2)
            )
        if Worlds.GetAmplifyIcons(effect) > 0:
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.ReadyAll(targets, effect)
                ).SetTarget("YourAlly", canbe_ready=True)
            )
        if Worlds.GetHazardIcons(effect) > 0:
            initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            i_got_this,
            conditions=[
                lambda effect, message:
                    Worlds.GetIconsNum(effect, ["Crisis", "Acceleration", "Amplify", "Hazard"]) > 0
            ],
        ).SetPlay().SetLabel(),
    ]

