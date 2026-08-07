from . import *

# * Cap's Shield

def GetAbilities() -> Sequence['Ability']:

    def caps_shield(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        this.AttachTo2(message.attacker, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Captain America")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            retaliate=1
        ),
        AbilityFactory.UnitAttackGainKeyword(
            None,
            "AttachedCharacter",
            lost_piercing=True,
        ),
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            CardFinder(card_type=Hero)|CardFinder(name="Captain America", card_type=Leader),
            "AttachedCharacter",
            caps_shield,
            use_atk=True,
        ),
    ]

