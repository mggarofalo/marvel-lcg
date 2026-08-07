from . import *

# A.I.M. Interference ([energy])

def GetAbilities() -> Sequence['Ability']:

    def aim_interference_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        cost_dict = {
            "50184a": "Y",
            "50184b": "B",
            "50184c": "R",
        }

        members = Worlds.FindCardsOnField(effect, trait="BOARD MEMBER")
        size = len(members)
        cost = cost_dict[this.paper.card_id]

        def prevent(targets: Sequence['CardFace'], res: 'Resources'):
            nonlocal members
            size = res.val
            targets = targets[:size]
            members = [x for x in members if x not in targets]

        player.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbilityWithCost(
                Cost.FromText(cost * size, up_to=True),
                None,
                prevent
            ).SetTarget(members, range=(0, "All"))
        )

        Faces.PlaceCountersOn(members, 1, 'secret', effect)

    def aim_interference_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        Faces.ResolveAbility([this], player, AbilityType.WhenRevealed, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            aim_interference_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            aim_interference_boost
        ),
    ]

