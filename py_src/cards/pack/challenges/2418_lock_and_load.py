from . import *

# Lock and Load

def GetAbilities() -> Sequence['Ability']:

    def lock_and_load(effect: 'Effect', message: 'Message.WhenMainSchemeWouldAdvance'):
        this = effect.this.CastTo(Challenge)
        Unused(this)

        # During setup, shuffle four random Weapon cards from these modular sets together to form the Experimental Weapons deck.
        # Shuffle the remaining cards into the encounter deck as normal.
        # encounter_sets = Worlds.GetModularSets(effect)
        faces = Worlds.GetEncounterDeck(effect).FindCards(
            trait="WEAPON",
            non_set_name="Crossbones"
        )
        faces = Rand.RandomChoice2(faces, 4, effect)
        from cards.pack.trors.crossbones import GetExperimentalWeapons
        Faces.ShuffleAllTo(faces, GetExperimentalWeapons(effect).deck, effect)

    return [
        AbilityFactory.WhenMainSchemeWouldAdvance(
            AbilityType.Challenge,
            MainScheme,
            lock_and_load,
            conditions=[
                lambda effect, message:
                    not Worlds.Phase(effect).IsGameStarted()
            ],
        )
    ]

