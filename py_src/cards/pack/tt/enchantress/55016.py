from . import *

# * Crown of the Enchantress

def GetAbilities() -> Sequence['Ability']:

    def crown_of_the_enchantress(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            faces = Filter.ByFinder(player.GetPlayerAreaCards(), CardFinder(trait="ENCHANTMENT"))
            face = Filter.One(faces, effect)
            if face:
                Faces.PlaceCountersOn([face], 1, 'charm', effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Enchantress")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Enchantress"),
            stalwart=1,
            expert_mode_only=True
        ),
        AbilityFactory.AfterUnitSchemeAgainst(
            AbilityType.ForcedResponse,
            CardFinder(name="Enchantress"),
            "You",
            crown_of_the_enchantress
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        )
        .SetCostFunc(CostFunc.Exhaust(Select.From("Identity", range=
            lambda effect: len(effect.world.players)
        )))
        .SetCostFunc(CostFunc.TakeDamage(1, Select.From("Identity", range="All"))),
    ]

