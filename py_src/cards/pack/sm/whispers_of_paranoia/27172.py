from . import *

# Old Grudge

def GetAbilities() -> Sequence['Ability']:

    def old_grudge_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            include_set_aside=True,
            card_type=Minion,
            is_nemesis=player,
        )
        if face:
            face.Reveal(player, effect)
            this.AttachTo2(face, effect)


    def old_grudge_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        this.DealDamage(player.GetControlCharacters(), 1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health=1,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            old_grudge_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            old_grudge_boost
        ),
    ]

