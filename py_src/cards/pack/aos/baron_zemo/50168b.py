from . import *

# The Accusation

def GetAbilities() -> Sequence['Ability']:

    def the_accusation_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        buff = this.GetBuff(Buff_50168a)

        means = buff.means
        motive = buff.motive
        opportunity = buff.opportunity
        accused = buff.accused

        # ~~1. Use the cards in the A.I.M. envelope to identify the mole.~~
        mole = CalcMole(effect)

        deck = Worlds.ScenarioDeck(effect, "A.I.M.Envelope")
        faces = deck.deck.GetAll()
        card_ids = [x.paper.card_id for x in faces]
        for face in faces:
            face.CastTo(Evidence).AttachTo2(mole, effect)

        # 2. For each guess you got wrong, place 1 secret counter on each [[Board Member]] card.
        wrong = 0
        if means not in card_ids:
            wrong += 1
        if motive not in card_ids:
            wrong += 1
        if opportunity not in card_ids:
            wrong += 1
        PlaceSecretCounterOnEachBoardMemberCard(wrong, effect)

        # 3. If you accused the wrong board member, place 3 secret counters on the accused.
        if accused != mole:
            Faces.PlaceCountersOn([accused], 3, 'secret', effect)

        # 4. Advance to stage 3A.
        this.Advance("3A", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_accusation_revealed
        ),
    ]

