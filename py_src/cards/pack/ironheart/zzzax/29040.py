from . import *

# Zzzap!

def GetAbilities() -> Sequence['Ability']:

    def zzzap_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        damage = FacesCounter.GetPrintedResourcesCount(player.hand_cards.GetAll(), "Y")

        gain_surge = True

        def action(took_message: 'Message.AfterUnitTookDamage'):
            nonlocal gain_surge
            if took_message.trigger == identity and took_message.deal_damage > 1:
                gain_surge = False

        identity.TakeIndirectDamage(this, damage, effect, operation=action)

        if gain_surge:
            ThisCardGainSurge(effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            zzzap_revealed
        ),
    ]

