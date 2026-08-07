from cards.pack import *

def AbsorbingManGainsTraitOfEnvironment():

    return AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
        "This",
        get_new_value=lambda effect, message:
            [y for x in Worlds.GetOnFieldEnvironment(effect) for y in x.traits],
        trait=1,
        change_on_event=OnEvent.Trait(Environment)
    )

def AbsorbingManHasTrait(effect: 'Effect', *traits: 'CardFace.TRAITS'):
    villain = GetAbsorbingMan(effect)
    for trait in traits:
        if villain.HasTrait(trait):
            return True
    return False

def GetAbsorbingMan(effect: 'Effect'):
    villain = Worlds.FindVillain(
        effect,
        "Absorbing Man",
    )
    assert villain
    return villain

def AfterAbsorbingManMakesUndefendedAttack(action: Callable[[Effect, Message.AfterUnitAttackUnit], Any]):
    return AbilityFactory.AfterUnitAttackUnit(
        AbilityType.ForcedResponse,
        Villain,
        "You",
        action,
        is_undefended_attack=True,
    )

