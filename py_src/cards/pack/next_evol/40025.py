from . import *

# * Deathlok: Unit L17

def GetAbilities() -> Sequence['Ability']:

    def deathlok(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        for face in effect.targets:
            if Upgrade.IsType(face):
                face.AttachTo2(this, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.HeroResponse,
            "This",
            deathlok
        ).SetTarget(
            CardFinder(
                card_type=Upgrade,
                cost_less_than=1,
                check_effect_fn=lambda effect, face: face.CastTo(Upgrade).CanAttachTo(effect.this),
            ),
            from_where=["PlayersDiscardPile"]),
    ]

