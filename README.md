# TestRandomBrush - Unity Tilemap RandomBrush Tests

Adds rotation and flipping and probability to RandomBrush.
Adding rotation and flipping to the 6 basic floor tiles effectively quadruples the amount of tiles.
Using probability you can make some tiles more rare then others so they get chosen less.
Includes simple example scene and tile set and palette.

## DefaultBrush Bugs

- [x] rotate & flip works for box but not flood fill
- [x] change size of brush with ctrl and paint not working

## RandomBrush Bugs

- [x] box & flood fill not working (and expanding)
- [x] not saving tiles from preview to use in paint
- [x] not using CreateAssetMenu
- [x] not showing in inspector
- [x] move not working

## RandomBrush Features

- [x] choose random brush based on probability
- [x] randomize the transform too
