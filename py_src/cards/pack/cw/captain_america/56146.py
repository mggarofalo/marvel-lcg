from . import *

# Shield Block

def GetAbilities() -> Sequence['Ability']:

    def shield_block_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        shield = Find.Find(
            effect,
            name="Cap's Shield",
            card_type=Attachment,
        )
        if shield:
            cap = Worlds.FindCardOnField(effect, name="Captain America", card_type=Leader)
            if cap:
                if shield.bind_face == cap:
                    Faces.GiveStatus([cap], "Tough", effect)
                    Faces.GiveFacedownBoostCards([cap], 1, effect)
                else:
                    shield.AttachTo2(cap, effect)
                    Faces.GiveFacedownBoostCards([cap], 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            shield_block_revealed
        ),
    ]

