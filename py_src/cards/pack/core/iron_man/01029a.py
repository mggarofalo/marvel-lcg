from . import *

# * Iron Man

def GetAbilities() -> Sequence['Ability']:

    def iron_man(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Hero)

        player = this.GetControlByPlayer()
        faces = player.GetControlCardsByType(CardFinder(trait="TECH"), upgrade=True)
        for face in faces:
            if face.HasTrait("TECH"):
                ui.append(face)
        return min(6, len(faces))

    return [
        AbilityFactory.ThisGainKeyword(
            iron_man,
            hand_size=1,
            change_on_event=OnEvent.AttachedMove(Upgrade)
        )
    ]

