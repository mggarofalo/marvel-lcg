from . import *

# "There is No Escape"

def GetAbilities() -> Sequence['Ability']:

    def there_is_no_escape(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        units = Worlds.GetPlayersIdentities(effect)
        this.DealDamage(units, 1, effect)


    return [
        AbilityFactory.StandardModeOnly(),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            there_is_no_escape,
        ),
    ]

