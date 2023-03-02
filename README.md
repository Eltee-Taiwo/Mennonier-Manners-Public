# Mennonier-Manners-Public

### https://mennonitemanners.com

This is a public facing version of the Mennonite Manners App and is intended to give anyone curious what the code structure of everything needed to successfully run the app. Do not expect it to run properly as sensitive information like API urls and authentication keys have been removed. If you are interested in seeing this app run, please [download it from your nearest app store](https://mennonitemanners.com/#download)!


## TaiwoTech.MennoniteManners.App

This folder contains all the code for the .Net MAUI mobile app. It highlights the MVVM model that is used to structure the code, the use of dependency injection, and how the real time connection with the signalR hub is maintained.

## Relevant API files

These files in this folder have benn pulled out of the Mennonite Manners API to show only the necessary parts that are required for the app to run.

- The controller shows how the app retrieves the basic settings required to run.
- The signalR hub shows how all the real time connectvity is handled for the game.
- The dataservices show how all game state information is stored and retrieved from a database with a cache system to reduce the number of hits to the database.


I hope the information in this Repo helps with whatever you're looking for.


Cheers!