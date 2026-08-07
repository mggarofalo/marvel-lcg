from . import *

# Fighting Zemo

def GetAbilities() -> Sequence['Ability']:

    def fighting_zemo_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        enemy = Worlds.FindCardOnField(
            effect,
            name="Baron Zemo",
            card_type=Unit2
        )
        assert enemy
        enemy.FlipTo(effect, trait="UNMASKED")
        enemy = enemy.card.face.CastTo(Villain)
        enemy.ResetHealth(effect, "Printed")

        face = Find.Find(effect, name="Baron Zemo's Sword", card_type=Attachment)
        if face:
            face.AttachTo2(enemy, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            fighting_zemo_revealed
        ),
    ]

