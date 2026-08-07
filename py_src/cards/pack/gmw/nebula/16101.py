from . import *

# Combat Ready

def GetAbilities() -> Sequence['Ability']:

    def combat_ready_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Attachment,
            trait="TECHNIQUE",
        )
        if face:
            face.Reveal(player, effect)
            face.ResolveSpecialAbility(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            combat_ready_revealed
        ),
    ]

