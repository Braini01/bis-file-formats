# Utility to manipulate WRP files.

You need usually 2.5GB free memory for most operations (everything is agressively loaded into memory for performance).

Remarks: If your are not the author of the source files, you have to ensure that files licence allows you to create a derivate work.
You must contact author if your are not sure about this. 
Allowed by APL-SA, APL, CC BY-NC-SA. 
Forbidden by APL-ND, Bohemia Interactive EULA.

## wrputil convert

Allows you to convert a binarised file to a file that can be imported into Terrain Builder (like the ConvertWrp utility but with fewer supported formats).

Can save your life, if your Terrain Builder projet was corrupted or lost (I hope you still have the imagery, it can't be restored).

It can also import maps from older Arma versions.

Like stated earlier, you must ensure that you can create a derivate work, check twice the licence !

Can read any OPRW or 8WVR, can write only 8WVR. 

## wrputil merge

Allows you to work in Terrain Builder with multiple projets, with fewer objects, and merge back to a single WRP to binarize. It allows to work with multiple authors on the
same map without sharing the Terrain Builder mess.

If you split in more than two parts, you will have to chain the calls.

## wrputil strip

Create a WRP without any object, an empty world. Can be usefull with `wrputil merge`.

## wrputil dependencies

Compute real dependencies of map, and generates a report will all dependencies.
