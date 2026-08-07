from . import *

# Hyper Thrusters

def GetAbilities() -> Sequence['Ability']:

    def hyper_thrusters(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        targets = effect.targets[:]

        initiator = effect.GetInitiator()
        if initiator.IsControl(CardFinder(name="Milano")):
            scheme = Worlds.FindMainScheme(effect)
            if scheme:
                targets.append(scheme)

        this.RemoveThreatFromSchemes(targets, 1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            hyper_thrusters
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Scheme2, range="All"),
    ]

