# Redirection

Redirection is a [puzzle game](http://www.redirectiongame.com) released in 2016 by [Daniel Ratcliffe](http://www.twitter.com/DanTwohundred). This repository contains the full source code of that game as currently shipped on Steam and itch.io.

Important Note: This source code release does not include any of the non-code assets from Redirection. These must be copied from a legally purchased copy of Redirection, which can be found on [Steam](https://store.steampowered.com/app/305760/Redirection/) or [Itch.io](https://dan200.itch.io/redirection).

This repository is intended to represent the game exactly as released, so will not be accepting any pull requests. Feel free to fork though!

# How to build this code

1) Clone the repository.
2) Copy the "assets" folder from a legally purchased copy of Redirection into the "Redirection" folder of the repository
3) Compile the Visual Studio solution provided. The "Debug" and "DebugWindows" configurations are for testing within Visual Studio, and the "Release" and "ReleaseWindows" configurations are for producing releasable builds.
4) Copy the contents of the "Redirection/Natives" subfolder for your OS into the "Redirection/bin" subfolder for your chosen configuration.
5) Run the game and have fun!
