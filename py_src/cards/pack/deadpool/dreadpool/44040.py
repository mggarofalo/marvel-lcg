from . import *

# Anti-Regeneration Ray

def GetAbilities() -> Sequence['Ability']:

    def anti_regeneration_ray(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        target = message.target
        target.TreatAsIfBlank(effect, message)

    def anti_regeneration_ray_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        this.AttachTo2(identity, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Dreadpool"),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.ForcedInterrupt,
            "AttachedCharacter",
            CardFinder(non_card_type=Villain),
            anti_regeneration_ray
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            anti_regeneration_ray_action
        ).SetCost(Cost("YBR"))
    ]

