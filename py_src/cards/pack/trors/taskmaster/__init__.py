from cards.pack import *

def AfterPlayerChangesToHeroFormDiscardTheEncounterDeckAndTakeDamage():
    def discard_the_top_card_of_the_encounter_deck_and_take_damage(effect: 'Effect', message: 'Message.AfterUnitChangeForm'):
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        hero = message.trigger.GetControlByPlayer().GetHero()
        faces = Worlds.DiscardEncounterCards(1, effect)
        damage = FacesCounter.CountTotalBoostIcons(faces)
        hero.TakeDamage(this, damage, effect)

    return AbilityFactory.AfterUnitChangeForm(
        AbilityType.ForcedResponse,
        None,
        discard_the_top_card_of_the_encounter_deck_and_take_damage,
        to_form=Hero,
    )

