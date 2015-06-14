# ScrapeMonet
A scraper for saving gifs and other stuff from CacheMonet.com

## Overview

ScrapeMonet is a small utility for scraping gifs, images, music and other resources from the online, web powered artwork, CacheMonet. It was created out of a misc of desparate study procrastination and extreme boredom, and is written entirely in C#.

## Operation

ScrapeMonet creates three folders: center, bg, and misc.

center is the destination directory all the gifs that are displayed randomly in the center of the CacheMonet window.
bg is the destination directory all the gifs that are displayed tiled in the background of the CacheMonet window.
misc is for any images or music linked anywhere in the main html of the CacheMonet index page.

## Under the hood

ScrapeMonet uses WebClient to download all web resources used during the scrape. JSON.NET is used to process the json gif lists that cachemonet uses. The scraper relies heavily on the asynchronous programming features (Task and async) only available in C# 5+ and .NET 4.5+ , however this allows it to easily perform all web downloads with a large amount of parallelism. 
