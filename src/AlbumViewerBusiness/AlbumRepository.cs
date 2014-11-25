﻿using Microsoft.Data.Entity;
using System;
using System.Linq;
using System.Threading.Tasks;
using Westwind.BusinessObjects;
using Westwind.Utilities;

namespace AlbumViewerBusiness
{
    public class AlbumRepository : EntityFrameworkRepository<MusicStoreContext,Album>
    {    
        public AlbumRepository(MusicStoreContext context)
            : base(context)
        { }
        
        public async Task<Album> SaveAlbum(Album postedAlbum)
        {
            int id = postedAlbum.Id;

            Album album = null;
            if (id < 1)
                album = Context.Albums.Add(new Album());
            else
            {
                album = await Context.Albums
                    .Include(ctx => ctx.Tracks)
                    .Include(ctx => ctx.Artist)
                    .FirstOrDefaultAsync(alb => alb.Id == id);
            }

            // check for existing artist and assign if matched
            if (album.Artist.Id < 1)
            {
                var artist = await Context.Artists
                                          .FirstOrDefaultAsync(art => art.ArtistName == postedAlbum.Artist.ArtistName);
                if (artist != null)   
                    album.Artist.Id = artist.Id;
            }

            Westwind.Utilities.DataUtils.CopyObjectData(postedAlbum.Artist, album.Artist, "Id");
            
            if (album.Artist.Id < 1)
                Context.Artists.Add(album.Artist);
            

            int result = Context.SaveChanges();

            album.ArtistId = album.Artist.Id;
            Westwind.Utilities.DataUtils.CopyObjectData(postedAlbum, album, "Tracks,Artist,Id,ArtistId");
           
            result = Context.SaveChanges();

            int albumId = album.Id;

            foreach (var postedTrack in postedAlbum.Tracks)
            {
                var track = album.Tracks.FirstOrDefault(trk => trk.Id == postedTrack.Id);
                if (postedTrack.Id > 0 && track != null)
                    Westwind.Utilities.DataUtils.CopyObjectData(postedTrack, track);
                else
                {
                    track = new Track();
                    Context.Tracks.Add(track);
                    Westwind.Utilities.DataUtils.CopyObjectData(postedTrack, track, "Id,AlbumId,ArtistId");
                    album.Tracks.Add(track);
                    track.AlbumId = albumId;
                }
                
            }

            // find tracks to delete - first looks for those posted (except 0 ids)
            var postedIds = postedAlbum.Tracks
                .Where(t => t.Id > 0)
                .Select(t => t.Id)
                .ToList();

            // then delete all those that don't exist in the actual albums
            var deletedTracks = album.Tracks
                .Where(trk => trk.Id > 0 && !postedIds.Contains(trk.Id))
                .ToList();


            if (deletedTracks.Count > 0)
            {
                foreach (var dtrack in deletedTracks)
                {
                    Context.Tracks.Remove(dtrack);
                    // BUG: this bonks!
                    //album.Tracks.Remove(dtrack);
                }
            }

            if (!await SaveAsync())
                return null;

            return album;
        }

        /// <summary>
        /// Pass in an external instance of an artist and either
        /// update or create that artist as an instance
        /// </summary>
        /// <param name="postedArtist"></param>
        /// <returns></returns>
        public async Task<Artist> SaveArtist(Artist postedArtist)
        {
            int id = postedArtist.Id;

            Artist artist;
            if (id < 1)
                artist = Create<Artist>();
            else
            {
                artist = Context.Artists.FirstOrDefault(a => a.Id == id);
                if (artist == null)
                    artist = Create<Artist>();
            }

            DataUtils.CopyObjectData(postedArtist, artist, "Id");

            if (!await SaveAsync())
                return null;

            return artist;
        }

    }

}