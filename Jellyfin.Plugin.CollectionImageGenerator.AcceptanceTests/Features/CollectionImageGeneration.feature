Feature: Collection Image Generation
    As a Jellyfin server administrator
    I want collections without images to have collages auto-generated
    So that my library has a polished appearance

    Scenario: Grid dimensions adapt to item count of 1
        Given a collection has 1 item with images
        Then the grid dimensions should be 1 row and 1 column

    Scenario: Grid dimensions adapt to item count of 2
        Given a collection has 2 items with images
        Then the grid dimensions should be 1 row and 2 columns

    Scenario: Grid dimensions adapt to item count of 3
        Given a collection has 3 items with images
        Then the grid dimensions should be 1 row and 3 columns

    Scenario: Grid dimensions adapt to item count of 4
        Given a collection has 4 items with images
        Then the grid dimensions should be 2 rows and 2 columns

    Scenario: Collage positions match image count
        Given a collection has 5 items with images
        Then the collage layout should contain 5 positions

    Scenario: Large collections cap at 9 grid positions
        Given a collection has 12 items with images
        Then the collage layout should contain 9 positions

    Scenario: Default configuration enables scheduled task
        Given the default plugin configuration
        Then the scheduled task should be enabled

    Scenario: Default configuration sets max images to 4
        Given the default plugin configuration
        Then the max images in collage should be 4
