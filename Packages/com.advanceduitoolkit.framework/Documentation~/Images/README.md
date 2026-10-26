# README screenshot plan

The main GitHub README contains six **intentional screenshot slots**. Capture screenshots from a real Unity project and save them here with these exact names:

| File | What to capture | Suggested crop |
| --- | --- | --- |
| `01-installation.png` | Package Manager with the installed toolkit and samples | 16:9 or 4:3 |
| `02-factory-hierarchy.png` | Factory alongside a correctly named UI Hierarchy | 16:9 |
| `03-binder-scan.png` | Binder showing discovered components and zero errors | 4:3 |
| `04-generated-code.png` | Binder's C# preview or generated class in code editor | 16:9 |
| `05-playmode-test.png` | Weapon Panel in Play Mode, with Console events | 16:9 |
| `06-style-profiles.png` | Sonar and Main Menu profiles/prefabs demonstrating reuse | 16:9 |

## How to insert images in README.md

Replace each screenshot placeholder quote block with a Markdown image pointing to the matching path:

```markdown
![Binder discovers the WeaponPanel controls](Documentation~/Images/03-binder-scan.png)
```

Do not use invented Unity screenshots or generic graphics as evidence of actual Editor behavior.
