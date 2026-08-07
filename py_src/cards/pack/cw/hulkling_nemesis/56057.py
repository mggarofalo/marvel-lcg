from . import *

# Fantastic Powers

def GetAbilities() -> Sequence['Ability']:

    def fantastic_powers(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()

        face = Find.FindAndPutIntoPlay(
            effect,
            player,
            name="Super Skrull",
            card_type=Minion
        )

        if face:
            this.AttachTo2(face, effect)

            if player.IsInHeroForm():
                face.DoAttackYou(player, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health=3,
            retaliate=1,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            fantastic_powers
        ),
    ]

