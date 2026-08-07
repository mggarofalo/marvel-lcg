from . import *

# "Take What Is Mine"

def GetAbilities() -> Sequence['Ability']:

    def take_what_is_mine_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        face = GetPowerStone(effect)
        if face:
            villain = Worlds.FindCardOnField(effect, name="Ronan")
            if villain:
                if face.bind_face == villain:
                    Faces.GiveFacedownBoostCards([villain], 1, effect)
                else:
                    face.AttachTo2(villain, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            take_what_is_mine_revealed
        ),
    ]

