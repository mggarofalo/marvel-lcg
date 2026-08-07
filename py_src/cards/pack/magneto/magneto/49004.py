from . import *

# * Magneto's Armor

def GetAbilities() -> Sequence['Ability']:

    def magnetos_armor(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()

        faces = identity.GetBuff(Buff_49001a).faces

        attack = 0
        defense = 0
        thwart = 0
        for face in faces:
            res = FacesCounter.GetPrintedResources([face])
            if res.b:
                thwart += 1
            if res.r:
                attack += 1
            if res.y:
                defense += 1

        identity.GainUntilRoundEnd(
            effect,
            attack=attack != 0,
            thwart=thwart != 0,
            defense=defense != 0,
        )

    return [
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.Response,
            "You",
            None,
            magnetos_armor,
            ability_name="Magnetic Pull",
            control_by_you=True,
        ),
    ]

