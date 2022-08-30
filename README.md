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

ScrapeMonet uses HttpClient to download all web resources found during the scrape. Resource paths are parsed out of the json resource blobs that cachemonet uses. Downloads occur in parallel to speed up scraping.