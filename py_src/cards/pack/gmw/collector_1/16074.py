from . import *

# Biogram Image

def GetAbilities() -> Sequence['Ability']:

    def biogram_image(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        value = message.PreventDamage("All", effect)
        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], value, effect)

    def biogram_image_cost(targets: Sequence['CardFace'], effect: 'Effect'):
        PutCardIntoTheCollection(targets, effect)
        return True


    def biogram_image_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        message.AfterThisActivation(effect, lambda: this.Reveal(player, effect))


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Collector")
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Collector"),
            biogram_image
        ).SetCostFunc(CostFunc.Custom(Select.From("This"), biogram_image_cost)),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            biogram_image_boost
        ),
    ]

