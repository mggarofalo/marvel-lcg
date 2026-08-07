from . import *

# Burning Hunger

def GetAbilities() -> Sequence['Ability']:

    def burning_hunger(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        identity = player.GetIdentity()
        if identity.HasTrait("UNLEASHED"):
            dark_phoenix = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                include_set_aside=True,
                name="Dark Phoenix"
            )
            if dark_phoenix:
                dark_phoenix.Reveal(player, effect)
            Faces.RemoveAllFromGame([this], effect)

        if identity.HasTrait("RESTRAINED"):
            face = Worlds.FindCardOnField(
                effect,
                name="Phoenix Force")
            if face:
                Faces.RemoveCountersOn([face], 1, 'power', effect)
            ThisCardGainSurge(effect)

            Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.WhenThisObligationRevealed(
            burning_hunger
        ),
    ]

