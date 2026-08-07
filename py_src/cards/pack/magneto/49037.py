from . import *

# Children of the Atom

def GetAbilities() -> Sequence['Ability']:

    TRAITS: List["CardFace.TRAITS"] = ["X-FACTOR", "X-FORCE", "X-MEN"]

    def check(effect: 'Effect', face: 'CardFace') -> bool:
        for trait in face.traits_internal:
            if trait in TRAITS:
                all_from_this_effect = [x for x in face.traits_internal[trait] if x.paper.card_id == effect.this.paper.card_id]
                if face.traits_internal[trait] != all_from_this_effect:
                    return True
        return False

    return [
        AbilityFactory.CanPlayThisSupportCard(
            under_any_players_control=True
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(traits=TRAITS, card_type=Unit2, check_effect_fn=check),
            control_by="You",
            trait=TRAITS,
            change_on_event=OnEvent.Trait("YouControlCharacter")
        ),
    ]

