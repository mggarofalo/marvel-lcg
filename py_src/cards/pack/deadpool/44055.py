from . import *

# Laser Swords

def GetAbilities() -> Sequence['Ability']:

    def laser_swords(effect: 'Effect', face: 'CardFace', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Upgrade)
        if not this.bind_face:
            return 0

        faces: List['CardFace'] = []
        if Hero.IsType(this.GetBindFace()):
            faces += Worlds.GetCrisisFaces(effect)
            faces += Worlds.GetAccelerationIconFaces(effect)
            faces += Worlds.GetAmplifyFaces(effect)
            faces += Worlds.GetHazardFaces(effect)

            ui.extend(faces)
            value = Worlds.GetIconsNum(effect, ["Crisis", "Acceleration", "Amplify", "Hazard"])
            return min(4, value)
        else:
            return 0

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            get_new_value=laser_swords,
            attack=1,
            ex_change_on_event=OnEvent.Icons("InPlay")
        ),
    ]

