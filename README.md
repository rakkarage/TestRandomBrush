# TestTileMap - Unity Tilemap Tests

## DefaultBrush Bugs

- [x] rotate & flip works for box but not flood fill
- [ ] move without selection draws white box
- [ ] change size of brush with ctrl and paint with long brush last position stamp has only 1 tile? this is broke in base too
- [ ] pivot etc. private?
  - would help with implementing some of this stuff?
- [ ] BoundsInt missing Encapsulate and Expand etc.
- [ ] my flood fill does not expand map like base!?
  - Tilemap.cellBounds is readonly so how can I expand map for flood fill like map.FloodFill does?

## RandomBrush Bugs

- [x] box & flood fill not working
- [x] not saving tiles from preview to use in paint
- [x] not using CreateAssetMenu
- [x] not showing in inspector
- [ ] move????????????????????????????????????

## RandomBrush Features

- [x] choose random brush based on probability
- [x] randomize the transform too

### TODO

- [ ] flood fill does not fill closed loop wall - IT DOES but requires tiles to match exactly mine allows any but should be fixed!
