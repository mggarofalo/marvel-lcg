from . import *

# Harpoon's Harpoon

def GetAbilities() -> Sequence['Ability']:

    def harpoons_harpoon(effect: 'Effect', player: 'Player') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Harpoon",
            card_type=Minion,
        )
        if face:
            face.Reveal(player, effect)

        do_surge = True
        if face:
            if this.AttachTo2(face, effect):
                do_surge = False
        if do_surge:
            ThisCardGainSurge(effect)

    def harpoons_harpoon_response(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            identity = player.GetIdentity()
            identity.TakeIndirectDamage(this, 2, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Harpoon"),
            if_cannot_operation=harpoons_harpoon,
        ),
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            CardFinder(name="Harpoon"),
            harpoons_harpoon_response,
        ),
    ]

