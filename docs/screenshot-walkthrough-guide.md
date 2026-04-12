# How to Create a Screenshot Walkthrough

Guide for creating form walkthroughs with screenshots in this AI-managed project.

## Image Location

Put screenshots in `docs/images/`. Name them descriptively:
- `colony-import-step1.png`
- `colony-structures-tab.png`
- `survey-form-filter.png`

## Workflow

1. Take your screenshots and drop them into `docs/images/`
2. Drag the images into the Kiro chat along with a prompt like:

> "Write a walkthrough for the Colony form. Here are screenshots of each step. The walkthrough goes in docs/colonies.md (replace the existing content). Reference the images using relative paths like `![description](images/colony-import-step1.png)`."

Kiro can see the screenshots, understand what's shown in each one, and write the walkthrough text with the correct image references.

## Tips

- Kiro can't take screenshots — you need to capture them
- Drag multiple images into a single message and say what each one shows, or Kiro will infer from the content
- If you want annotations (arrows, callouts), add those before dropping the images in — Kiro can't edit images
- Use relative paths in markdown (`images/filename.png`) so they work on GitHub and in local viewers

## In-App Help Compatibility

The in-app help system (FormHelp) reads markdown from the `docs/` folder at runtime. For images to render in the in-app help browser:
- Image paths must be relative (`images/filename.png`)
- The `docs/images/` folder needs to be copied to the output directory alongside the docs
- This may require a build step or post-build copy — the current help system doesn't handle image copying automatically
