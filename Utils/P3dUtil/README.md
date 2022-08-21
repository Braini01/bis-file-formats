# p3dutil

This tool can manipulate only MLOD files.

## Templating

This verb allows you to generate p3d files from a template file.

`p3dutil template <path to json file>`

### "per-texture" templating

This template mode allows you to generate a p3d for each paa file from a directory.

This can usefull to generated variations of a p3d file for road/town signs or any model that cannot use hiddenselection.

Sample for playing cards (as featured in one of my mods) :

`p3dutil template playingcards.json`

File `playingcards.json` (stored in `z\gtd\addons\playingcards`) :
```json
{
	"Mode": "per-texture",
	"TemplateFile": "data\\clubs\\1.p3d", // Template file
	"TextureBaseDirectory": "data", // Directory to search for paa files
	"TextureBaseGamePath": "z\\gtd\\addons\\playingcards\\data", // Engine path matching TextureBaseDirectory 
	"TexturePattern": "*_256.paa", // Pattern to use to search for paa files
	"TextureNameFilter": "_256.paa", // Used to generate target p3d file name
	"InitialTexture": "z\\gtd\\addons\\playingcards\\data\\clubs\\1_256.paa", // Texture in TemplateFile to replace
	"Backup": true // Generate ".bak" files if p3d already exists
}
```

File `huts.json` (stored in `z\arm\addons\common\data\houses`) :
```
{
	"Mode": "per-texture",
	"TemplateFile": "hut*.p3d", 
	"TextureBaseDirectory": "tex", 
	"TextureBaseGamePath": "z\\arm\\addons\\common\\data\\houses\\tex", 
	"TexturePattern": "*_co.paa", 
	"TextureNameFilter": "_co.paa", 
	"InitialTexture": "z\\arm\\addons\\common\\data\\houses\\tex\\hut_co.paa", 
	"Backup": true
}
```

## Path replace

This verb allows you to make a find and replace operation on texture and material paths of p3d file.

`p3dutil replace-path <p3d path> <old path> <new path>`

Sample : 
`p3dutil replace-path *.p3d "z\gtd\" "z\gtdi\"`

## UV transform

This verb allows you to make an affine transform on UV of a texture.

It can help to merge multiple textures into a single one to save space and reduce memory usage.

`p3dutil uv-transform <source> <target> --texture <texture path> --u-mul <Umul> --u-add <Uadd> --v-mul <Vmul> --v-add <Vadd>`

- U<sub>target</sub> = U<sub>source</sub> * U<sub>mul</sub> + U<sub>add</sub>
- V<sub>target</sub> = V<sub>source</sub> * V<sub>mul</sub> + V<sub>add</sub>

Sample : 
`p3dutil uv-transform model1.p3d model2.p3d --texture wall_co --u-mul -1 --u-add 1`

(Horizontal mirror on texture wall_co)
