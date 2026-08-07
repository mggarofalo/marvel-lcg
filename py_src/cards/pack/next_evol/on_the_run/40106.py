from . import *

# Hidden in the Clutter

def GetAbilities() -> Sequence['Ability']:

    def hidden_in_the_clutter(effect: 'Effect', message: 'Message.WhenFaceWouldDealDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)
        Faces.PlaceCountersOn([this], message.damage, 'damage', effect)

        if this.GetCounters('damage') >= 3:
            player = message.by_effect.initiator
            attached = this.GetBindFace()
            if Enemy.IsType(attached) and isinstance(player, Player):
                attached.DoAttackYou(player, effect)
                Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Enemy,
            fewest_remaining_hp=True
        ),
        AbilityFactory.WhenDamageWouldBeDealtTo(
            AbilityType.ForcedInterrupt,
            "AttachedEnemy",
            hidden_in_the_clutter,
        ),
    ]

