from . import *

# Shadow of the Past

def GetAbilities() -> Sequence['Ability']:

    def shadow_of_the_past(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.set_aside_nemesis_sets.Get()

        minions = Filter.ByType(faces, Minion, CardFinder(is_nemesis=player))
        faces = [x for x in faces if x not in minions]
        schemes = Filter.ByType(faces, SchemeSide2)
        faces = [x for x in faces if x not in schemes]

        entered = False
        def action():
            nonlocal entered
            entered = True

        for minion in minions:
            minion.Reveal(message.to_player, effect, if_entered_play=action)

        for scheme in schemes:
            scheme.Reveal(message.to_player, effect)

        # Fix for "26030"
        rest_faces: List[CardFace] = [x for x in faces if x.card.area == player.set_aside_nemesis_sets]
        encounter_deck = Worlds.GetEncounterDeck(effect)
        Faces.ShuffleAllTo(rest_faces, encounter_deck, effect, ui_group=False)

        if not entered:
            this.GainSurge(1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            shadow_of_the_past
        )
    ]
