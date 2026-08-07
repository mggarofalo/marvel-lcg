from . import *

# Heightened Morale

def GetAbilities() -> Sequence['Ability']:

    def get_villains_number(effect: 'Effect', face: 'CardFace', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Attachment)

        if this.bind_face == None:
            return 0

        faces = Worlds.GetVillains(effect)
        ui.extend(faces)
        return len(faces)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain,
            highest_order=True,
            if_cannot_operation=ResolveMainSchemeAmbushAndAttach
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Villain,
            get_new_value=get_villains_number,
            attack=1,
            ex_change_on_event=OnEvent.CardInPlay(Villain)
        )
    ]

