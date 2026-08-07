from . import *

# Full Auto

def GetAbilities() -> Sequence['Ability']:

    def full_auto_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        hero = player.GetHero()
        villain = Worlds.FindCardOnField(
            effect,
            name="Crossbones",
            card_type=Villain
        )
        if villain:
            faces = Worlds.DiscardEncounterCards(villain.attack, effect)
            boost = FacesCounter.CountTotalBoostIcons(faces)
            this.DealIndirectDamage(hero, boost, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            ThisCardGainSurge
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            full_auto_hero
        ),
    ]

