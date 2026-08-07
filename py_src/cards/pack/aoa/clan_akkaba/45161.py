from . import *

# Clan Akkaba Zealot

def GetAbilities() -> Sequence['Ability']:

    def clan_akkaba_zealot(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        scheme = Worlds.FindCardOnField(
            effect,
            name="Ancient Ritual",
            card_type=Scheme2,
        )
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 2, effect)

    def clan_akkaba_zealot_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        scheme = Worlds.FindCardOnField(
            effect,
            name="Ancient Ritual",
            card_type=Scheme2
        )

        if scheme:
            this.PlaceThreatOnSchemes([scheme], 1, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            clan_akkaba_zealot
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            clan_akkaba_zealot_boost
        ),
    ]

