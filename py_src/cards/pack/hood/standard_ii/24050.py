from . import *

# Dark Dealings

def GetAbilities() -> Sequence['Ability']:

    def dark_dealings_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoSchemes(player, effect, property=SchemeProperty(additional_value=1))


    def dark_dealings_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.GiveBoostCardForThisActivation(Villain, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            dark_dealings_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            dark_dealings_boost
        ),
    ]

