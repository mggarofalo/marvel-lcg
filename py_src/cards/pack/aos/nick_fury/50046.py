from . import *

# Secret Agent

def GetAbilities() -> Sequence['Ability']:

    def secret_agent(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        value = this.RemoveThreatFromSchemes(effect.targets, 1, effect)
        face = initiator.form.FindSuitForm()
        if face and value:
            Faces.PlaceTokensOn([face], value, 'threat', effect)

    return [
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.HeroResponse,
            "You",
            CardFinder2("PREPARATION"),
            secret_agent,
            control_by_you=True
        ).SetTarget(Scheme2),
    ]

