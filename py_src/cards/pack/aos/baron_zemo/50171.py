from . import *

# Reluctant Foe

def GetAbilities() -> Sequence['Ability']:

    def reluctant_foe_reveal(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()

        # Search your collection for a hero whose title does not match a character in play and put it into play engaged with you. Attach this card to it.
        units = Worlds.GetOnFieldCharacters(effect)
        titles = set([x.name for x in units] + [x.printed_subtitle for x in units if x.printed_subtitle != ''])
        from cards.paper import Paper
        def check(paper: 'Paper'):
            for player in Worlds.GetPlayers(effect):
                if player.IsName(paper.name):
                    return False
            return paper.name not in titles and paper.subtitle not in titles

        face = Search.Collection(effect, player, card_type=Hero, check_fn=check)
        if face:
            face.PutIntoPlay(player, effect)
            this.AttachTo2(face, effect)


    return [
        AbilityFactory.TreatAttachedCardAsMinion(
            Hero,
            "Elite Minion",
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            reluctant_foe_reveal
        ),
    ]

