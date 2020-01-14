# Toccata

> A Simple UWP Music Player for Touch-Screen Tablets

This is a Windows UWP app for playing audio files.  It is pretty simplistic, with design considerations as follows:
1) Targeting touch screen devices -> it's OK that some controls don't work smoothly with mouse / keyboard
2) Targeting low-performance devices -> it does the minimum of file-access, doesn't build a library, look up cover images etc.
3) Targeting tablets -> it handles landscape and portrait orientations
4) You have to organise your files under your 'Music' folder (because it's a UWP app, so it can't access files in the general file system)
5) It expects paths to be like ARTIST\ALBUM\TRACK with your music folder (because it doesn't build a library, or look up tags or anything - it just relies on your file/folder organisation)
6) It expects tracks files to have names beginning with digits, giving your their play order within the album (if it doesn't find such numbers, it will try to sort by track number from the music metadata instead)

## Support (and if you want to discuss contributing to the project)

Reach out to me at one of the following places!

- email at <a href="mailto:jim@turnipsoft.co.uk" target="_blank">`jim@turnipsoft.co.uk`</a>
- Twitter at <a href="http://twitter.com/turnipsoft" target="_blank">`@turnipsoft`</a>

## License

[![License](http://img.shields.io/:license-mit-blue.svg?style=flat-square)](http://badges.mit-license.org)

- **[MIT license](http://opensource.org/licenses/mit-license.php)**
- Copyright 2020 © <a href="http://www.turnipsoft.co.uk" target="_blank">Turnipsoft</a>.
