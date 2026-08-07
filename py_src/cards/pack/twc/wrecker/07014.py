from . import *

# I've Been Waiting For This!

def GetAbilities() -> Sequence['Ability']:

    def ive_been_waiting_for_this(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            villain.HealHealth(3, effect)

            Faces.GiveStatus([villain], "Tough", effect)

    def ive_been_waiting_for_this_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        scheme = GetSideSchemeWhoseHas(effect, least_threat=True)

        villain = scheme.FindCorrespondingVillain()
        if villain:
            villain.SetActive(effect)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoSchemes(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ive_been_waiting_for_this
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            ive_been_waiting_for_this_boost
        )
    ]

