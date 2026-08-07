from . import *

# Skies Over New York

def GetAbilities() -> Sequence['Ability']:

    def skies_over_new_york(effect: 'Effect', message: 'Message.GettingMainScheme') -> 'MainScheme|None':
        this = effect.this.CastTo(Environment)
        Unused(this)

        schemes = Worlds.GetMainSchemes(message.game_area)

        for scheme in schemes:
            if scheme.GetCounters('glider'):
                return scheme
        return None

    return [
        AbilityFactory.GetMainScheme(
            skies_over_new_york,
            conditions=[
                lambda effect, message:
                    Insert.IsType(message.by_face) or \
                    EncounterCard.IsType(message.by_face) or \
                    Villain.IsType(message.by_face) or \
                    MainScheme.IsType(message.by_face),
            ]
        )
    ]

