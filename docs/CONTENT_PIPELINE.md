# Content Pipeline (MVP)

## MVP Rules
- All fish/ship/hook content must be data-driven.
- Use stable string IDs; never use display names as IDs.
- New content should not require code changes where possible.

## File Naming
- ScriptableObjects: `SO_<Type>_<Id>.asset`
- Sprites: `<type>_<id>_<variant>_vNN.png`
- Audio: `<type>_<id>_<variant>_vNN.wav`

## Pivot Guidance
- Validate pivot conventions before linking into gameplay.
- Keep pivot positions consistent across same content class.

## Add New Fish Checklist
1. Create sprite/audio assets using naming rules.
2. Create `FishDefinition` asset with stable `id`.
3. Configure depth and distance constraints.
4. Register in game catalog.
5. Run validator and smoke test catch/sell flow.
