from . import *

# Golden Horse

def GetAbilities() -> Sequence['Ability']:

    def golden_horse(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.attacker.GetControlByPlayer()

        villain = message.attacked.CastTo(Villain)
        Faces.ResolveAbility([villain], player, AbilityType.ForcedResponse, effect)
        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(card_type=Villain, non_trait="AERIAL"),
            fewest_remaining_hp=True
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Villain,
            trait="AERIAL",
            considered_at_least_hp=1,
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.HeroResponse,
            Identity,
            "AttachedVillain",
            golden_horse
        ),
    ]

