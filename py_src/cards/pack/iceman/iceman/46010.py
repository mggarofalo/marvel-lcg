from . import *

# Ice Blast

def GetAbilities() -> Sequence['Ability']:

    def ice_blast(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        for target in effect.targets:
            face = GetSetAsideFrostbite(initiator)
            if face:
                face.AttachTo2(target, effect)
            else:
                break

        targets = Worlds.FindCardsOnField(
            effect,
            card_type=Enemy,
            with_attach=CardFinder(name="Frostbite"),
        )
        this.DealDamage(targets, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            ice_blast
        ).SetPlay().SetLabel()
        .SetTarget("VillainAndEngagedSamePlayerMinion"),
    ]

