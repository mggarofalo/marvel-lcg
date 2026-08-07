from . import *

# Yellowjacket's Plan

def GetAbilities() -> Sequence['Ability']:

    def yellowjackets_plan(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def is_from_ant_man_nemesis_set(face: 'CardFace') -> bool:
            for name in ['12026', '12027', '12028', '12029']:
                if face.IsName(name):
                    return True
            return False

        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=EncounterCard,
            check_face_fn=is_from_ant_man_nemesis_set,
        )
        if face:
            face.Reveal(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            yellowjackets_plan
        )
    ]

