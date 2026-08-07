from . import *

CONFUSED_FINDER = CardFinder(name="Confused")
STUNNED_FINDER = CardFinder(name="Stunned")
TOUGH_FINDER = CardFinder(name="Tough")

class Status(Component):
    @override
    def __init__(self, card: 'Card') -> None:
        from game.deck import Deck2, DeckType
        super().__init__(card)
        self.deck = Deck2(self.parent, DeckType.StatusArea, StatusCard)

    @property
    def confused(self):
        return self.deck.FindCardSize(CONFUSED_FINDER)

    @property
    def stunned(self):
        return self.deck.FindCardSize(STUNNED_FINDER)

    @property
    def tough(self):
        return self.deck.FindCardSize(TOUGH_FINDER)

    @override
    def OnParentReset(self):
        from game.effect.rule import GameRule
        from game.operate.faces import Faces
        Faces.DiscardAll(self.deck.GetAll(True), GameRule(self.parent.face))

    @override
    def OnDiscard(self, by_effect: 'Effect') -> List['CardFace']:
        self.deck.DiscardAll(by_effect)
        return super().OnDiscard(by_effect)

    def GiveStatusCard(self, name: "CardFace.STATUS", by_effect: 'Effect') -> 'StatusCard':
        from game.card.factory import CardFactory
        card = CardFactory.GenerateCard(name, self.deck, world=by_effect.world)
        return card.face.CastTo(StatusCard)

    def DiscardStatusCard(self, name: "CardFace.STATUS", by_effect: 'Effect', rule: 'CanStatus.DISCARD_RULE') -> int:
        from game.operate.faces import Faces

        this = self.parent.face.CastTo(Unit2)

        need_discard = Math.INT_INF # How much we need to discard

        if rule == "Steady": # When unit is activate with Steady keyword
            if name in ["Confused", "Stunned"]:
                if this.IsSteady():
                    need_discard = 2
                else:
                    need_discard = 1
            else:
                need_discard = 1
        elif rule == "All":
            # A: If Colossus activates the Response of Organic Steel, giving himself a new Tough status card, would Piercing trigger again and remove that new Tough?
            # Q: After a piercing attack discarded a Tough status card, you could activate Organic Steel and keep the new Tough status card. Piercing wouldn’t trigger a second time.
            need_discard = Math.INT_INF
        else:
            need_discard = rule
        status = self.deck.FindCards(name=name, card_type=StatusCard, max=need_discard)
        discard_size = len(Faces.DiscardAll(status, by_effect))
        return discard_size

    def GetStatusSize(self):
        return self.deck.GetSize()

    ################################################################################
    #
    def GetDeck(self) -> 'Deck2[StatusCard]':
        return self.deck

