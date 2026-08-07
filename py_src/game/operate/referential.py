from cards.database import CardsDB
from . import *

@final
class Referential:
    @staticmethod
    def BelongToSameIdentityEncounterSet(find_names: List[str], face: 'CardFace', legal_faces: List['CardFace']) -> List['CardFace']:
        face.card.IsAsOtherCard()

        check_faces: List[CardFace] = []
        set_name = face.paper.set_name
        # Fix "50061"
        set_name_nemesis = set_name[:-8] if set_name.endswith(" Nemesis") else None
        set_id = face.paper.card_id[:2]

        if set_name == "Iceman" and set_name_nemesis == None:
            set_name_nemesis = "Frostbite"

        has_this_name_in_set = False
        for card_id in CardsDB.sets_cards[set_name]:
            if CardsDB.papers[card_id].name in find_names:
                has_this_name_in_set = True
                break
        if not has_this_name_in_set:
            return legal_faces

        for target in legal_faces:
            if target.paper.set_name == set_name or \
                target.paper.set_name == set_name_nemesis:
                # Hack "Magneto"
                if set_name == "Magneto" or \
                    set_name == "Venom":
                    if target.paper.card_id[:2] != set_id:
                        continue
                check_faces.append(target)
        if check_faces:
            return check_faces
        return []

    @staticmethod
    def Filter(finder: 'CardFinder|None', legal_faces: List['CardFace'], by_effect: 'Effect') -> Sequence['CardFace']:
        if by_effect.world.rule.v16_referential_ability:

            if len(legal_faces) == 1:
                return legal_faces

            if not finder:
                return legal_faces

            check_by_name = finder.check_by_name

            if not check_by_name:
                return legal_faces

            this = by_effect.this

            # REFERENTIAL ABILITY
            # 1. The card on which the referential ability is printed.
            if this in legal_faces and finder.Check(this):
                return [this]

            # 2. Cards that belong to the same identity or encounter set.
            check_faces = Referential.BelongToSameIdentityEncounterSet(check_by_name, this, legal_faces)
            if check_faces:
                return check_faces

            # 3. Player cards (if the ability is on a player card) or
            # encounter cards (if the ability is on an encounter card).
            if PlayerCard.IsType(this):
                return [x for x in legal_faces if PlayerCard.IsType(x)]
            elif EncounterCard.IsType(this):
                return [x for x in legal_faces if EncounterCard.IsType(x)]
            return legal_faces
        else:
            return legal_faces

    @staticmethod
    def Check(finder: 'CardFinder', face: 'CardFace', from_effect: 'Effect') -> bool:
        from game.operate.worlds import Worlds

        if not finder.Check(face, from_effect):
            return False

        check_by_name = finder.check_by_name
        if not check_by_name:
            return True
        if not from_effect.world.rule.v16_referential_ability:
            return True

        # 1. The card on which the referential ability is printed.
        if from_effect.this.name == face.name and from_effect.this != face:
            return False

        # Hack for "90003"
        if not face.IsInPlay():
            return True

        # Hack for "01140"
        if face.card.IsAsOtherCard():
            return True

        # 2. Cards that belong to the same identity or encounter set.
        all_faces = Worlds.GetOnFieldCards(from_effect) + [y for x in Worlds.GetPlayers(from_effect) for y in x.dealt_encounter_cards.GetAll()]
        same_name_faces = [x for x in all_faces if x.IsFaceUp() and x.name == face.name]

        # Hack for "27081", "32087b"
        if len(same_name_faces) == 1 and \
            face.card == same_name_faces[0].card:
                return True

        found_faces = Referential.BelongToSameIdentityEncounterSet(check_by_name, from_effect.this, same_name_faces)
        return face in found_faces
        # 3. All other cards.
        # return True

