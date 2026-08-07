from . import *

# Lower Manhattan - B

def GetAbilities() -> Sequence['Ability']:

    def lower_manhattan(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        for scheme in Worlds.GetOnFieldSchemes(effect):
            value = 1
            symbiote = Worlds.FindCardOnField(
                effect,
                trait="SYMBIOTE",
                card_type=Environment
            )
            if scheme == this and symbiote:
                value += 1
            this.PlaceThreatOnSchemes([scheme], value, effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            lower_manhattan
        )
    ]

