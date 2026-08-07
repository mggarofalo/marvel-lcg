from . import *

# * Venom: Flash Thompson

def GetAbilities() -> Sequence['Ability']:

    def venom(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Ally)
        Unused(this)

        scheme = Worlds.FindMainScheme(effect)
        if scheme and scheme.threat == 0:
            ui.append(scheme)
            return 1
        return 0

    return [
        AbilityFactory.ThisGainKeyword(
            venom,
            attack_consequential_damage=-1,
            thwart_consequential_damage=-1,
            change_on_event=OnEvent.Threat(MainScheme)
        ),
    ]

