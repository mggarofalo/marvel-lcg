from . import *

# * Kang (The Conqueror) (I)

def GetAbilities() -> Sequence['Ability']:

    def kang_the_conqueror_i(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)
        Faces.RemoveAllFromGame([this], effect)

        def advance_main_scheme():
            scheme = Worlds.FindMainScheme(this)
            if scheme:
                scheme.Advance(None, effect)

        RunAt.TheEndOfThePhase(effect, advance_main_scheme)

    return [
        KangTheConquerorAttack(),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            kang_the_conqueror_i
        )
    ]

