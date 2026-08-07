from . import *

# Scarab

def GetAbilities() -> Sequence['Ability']:

    def scarab(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        scheme = Worlds.FindCardOnField(
            effect,
            name="Ancient Ritual",
            card_type=Scheme2
        )
        if scheme:
            value = 1
            if [x for x in message.damaged_targets if Ally.IsType(x)]:
                value = 3
            this.PlaceThreatOnSchemes([scheme], value, effect)

    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.NonKeywordStar,
            "This",
            scarab
        ),
    ]

