from . import *

# Hunting the Spider-Bride

def GetAbilities() -> Sequence['Ability']:

    def hunting_the_spider_bride_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        faces = identity.GetPlacedCardArea().Get()
        if len(faces) == 4:
            face = Rand.RandomChoice(faces, effect)
            Faces.DiscardAll([face], effect)

        identity.TuckCardUnderHere(this, effect)

    def hunting_the_spider_bride(effect: 'Effect', message: 'Message.AfterCardDiscard') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.from_area.GetOwnerPlayer()
        identity = player.GetIdentity()
        identity.TakeDamage(this, 2, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hunting_the_spider_bride_revealed
        ),
        AbilityFactory.AfterCardDiscardInternal(
            AbilityType.ForcedResponse,
            "This",
            hunting_the_spider_bride,
            from_where="UnderIdentity",
            by_effect_face=PlayerCard
        ),
    ]

