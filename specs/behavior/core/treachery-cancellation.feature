@core
Feature: Treachery cancellation
  Cancelling a treachery's When Revealed effects includes its Surge keyword.

  @behavior:card:01004:when-treachery-card-is-revealed-from-encounter
  @card:01004 @card:01191 @ruling:6b756402a689cade @rr:surge.1 @rr:cancel.1
  Scenario: Enhanced Spider-Sense cancels Exhaustion and its Surge
    # "Cancel abilities interrupt the initiation of effects and prevent them
    # from resolving." The ruling includes Surge among cancelled When Revealed
    # effects. Spider-Man stays ready and the next encounter card is not dealt,
    # while the revealed treachery, event and resource payment are discarded.
    # Expert Rhino starts with Breakin’ & Takin’: its hazard deals the
    # Sandman independently. Hydra Mercenary must remain in the encounter deck.
    Given a canonical Core scene is dealt
      | campaign     | heroes     | seed |
      | rhino_expert | spider_man | 725  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is ready
    And card 01097b copy 0 has 0 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01004 | 0    |
      | 01088 | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01191     | 0    |
      | 01102     | 0    |
      | 01101     | 0    |
    When villain phase 1 resolves accepting "Enhanced Spider-Sense" paid with card 01088 copy 0
    Then card 01001a copy 0 is ready
    And card 01097b copy 0 has 1 threat counter
    And card 01191 copy 0 is faceup on top of the encounter discard pile
    And card 01004 copy 0 is faceup on top of seat 1's discard pile
    And card 01088 copy 0 is in seat 1's discard pile
    And card 01102 copy 0 is engaged with seat 1
    And card 01101 copy 0 is in the encounter deck
