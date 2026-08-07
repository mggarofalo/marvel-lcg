from . import *

# Past Machinations

def GetAbilities() -> Sequence['Ability']:

    def past_machinations(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        face_names: List[str] = []

        def is_different(face: CardFace):
            nonlocal face_names
            if face.name not in face_names:
                return True
            return False

        def action(player: 'Player'):
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                card_type=Obligation,
                check_face_fn=is_different,
            )
            if face:
                face_names.append(face.name)
                face.Reveal(player, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            past_machinations
        )
    ]

