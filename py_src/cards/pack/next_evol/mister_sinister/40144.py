from . import *

# Sinister Disguise

def GetAbilities() -> Sequence['Ability']:

    def sinister_disguise(effect: 'Effect', message: 'Message.WhenFaceWouldDealDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.trigger.GetControlByPlayer()
        if not player.AskSpendResources(Cost("BB"), effect):
            message.SetBeInstead(effect)
            unit = Filter.One(Worlds.GetOnFieldFriendlyCharacters(effect), effect, fewest_remaining_hp=True)
            if unit:
                this.DealDamage([unit], message.damage, effect)
        Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Mister Sinister")
        ),
        AbilityFactory.WhenDamageWouldBeDealtTo(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Mister Sinister"),
            sinister_disguise,
            who_deal_damage="Player",
        ),
    ]

