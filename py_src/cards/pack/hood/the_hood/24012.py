from . import *

# Field Recruitment

def GetAbilities() -> Sequence['Ability']:

    def field_recruitment_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        AsideModularSet.ChooseRandom(effect, do_shuffle=True)

        ResolveFoulPlayerAbility(player, effect)

        Faces.RemoveAllFromGame([this], effect)


    def field_recruitment_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        def action():
            ResolveFoulPlayerAbility(player, effect)
        message.AfterThisActivation(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            field_recruitment_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            field_recruitment_boost
        ),
    ]

