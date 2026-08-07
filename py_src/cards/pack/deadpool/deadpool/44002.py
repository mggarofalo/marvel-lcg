from . import *

# * Cable: Nathan Summers

def GetAbilities() -> Sequence['Ability']:

    def get_acceleration_token(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Ally)
        schemes = Worlds.GetAllMainSchemes(effect)
        schemes = [x for x in schemes if x.card.game_area == this.card.game_area]
        if schemes:
            value, index = Math.MaxAndIndex([x.acceleration_token for x in schemes])
            if value > 0:
                scheme = schemes[index]
                ui.append(scheme)
        else:
            value = 0
        return min(3, value)

    return [
        AbilityFactory.ThisGainKeyword(
            get_acceleration_token,
            thwart=1,
            attack=1,
            change_on_event=OnEvent.AccelerationToken(MainScheme)
        ),
    ]

