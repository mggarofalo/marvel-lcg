from cards.pack import *

def GetResourceGeneratedBySyncRatio(effect: 'Effect') -> int:
    if isinstance(effect.bind_message, Message.AfterCardEnterPlay):
        effect = effect.bind_message.by_effect
    for paid_effect in effect.context.paid_this_res_effects:
        if paid_effect.IsName("Sync Ratio"):
            if isinstance(paid_effect.bind_message, Message.WhenPlayerPayingResources):
                return paid_effect.bind_message.generated_res.val
    return 0


def Spdr2() -> 'Ability':

    def action2(effect: 'Effect', message: 'Message.AfterCardFlip') -> None:
        this = effect.this
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()

        if initiator.IsHero():
            this.CastTo(Upgrade).AttachTo2(identity, effect)
        else:
            this.PutIntoPlay(initiator, effect)

    return AbilityFactory.AfterCardFlipToThisSide(
        AbilityType.ForcedInterrupt,
        action2,
    )

def Spdr() -> 'Ability':

    def action(effect: 'Effect', message: 'Message.WhenCardFlip') -> None:
        this = effect.this
        Unused(this)

        # def swap_ready_state(a: CardFace, b: 'CardFace'):
        #     a_ready = a.IsReady()
        #     b_ready = b.IsReady()

        #     if b_ready and not a_ready:
        #         Faces.ReadyAll([a], effect)
        #         Faces.ExhaustAll([b], effect)
        #     if not b_ready and a_ready:
        #         Faces.ExhaustAll([a], effect)
        #         Faces.ReadyAll([b], effect)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()
        health = identity.health
        # swap_ready_state(this, identity)
        new_identity = None
        if initiator.IsAlterEgo():
            # When you flip to this side, flip SP//dr Suit to its [[Active]] side. Attach this card to SP//dr Suit, moving all counters on this cards or cards attached to this card to SP//dr Suit.
            spdr = Worlds.FindCardOnField(
                effect,
                name="SP//dr Suit",
                card_type=Support
            )
            if spdr:
                spdr.FlipTo(effect, trait="ACTIVE")
                new_identity = spdr.card.CastTo(Hero)
        else:
            # When you flip to this side, flip SP//dr to Peni Parker. Detach Peni Parker from here, moving all counters on her and cards attached to her to this card.
            spdr = Worlds.FindCardOnField(
                effect,
                name="SP//dr",
                card_type=Upgrade
            )
            if spdr:
                spdr.FlipTo(effect, name="Peni Parker")
                new_identity = spdr.card.CastTo(AlterEgo)
        if new_identity:
            new_identity.PutIntoPlay(initiator, effect)
            for upgrade in identity.GetInventoryDeck().Get():
                upgrade.AttachTo2(new_identity, effect)
            for counter_name in identity.components.counter.GetCounterNames():
                size = identity.GetCounters(counter_name)
                new_identity.SetCounters(size, counter_name, effect)
            for token_name in identity.components.token.GetTokenNames():
                size = identity.GetTokens(token_name)
                new_identity.SetTokens(size, token_name, effect)
            identity.RemoveAllCounters(effect)
            identity.RemoveAllTokens(effect)
            new_identity.SetHealth(health, effect)
            status = identity.components.status.GetDeck().GetAll()
            Faces.MoveAllTo(status, new_identity.components.status.GetDeck(), effect)
        # spder = this.CastTo(Support)
        # spder.PutIntoPlay(player, effect, under_control=True)

    return AbilityFactory.WhenCardFlipToThisSide(
        AbilityType.ForcedInterrupt,
        "This",
        action,
    )

