using MongoDB.Driver;

namespace tvmaze_scraper.sync;

public static class Database
{
 // Return p
    public static SyncLock GetLock(this IMongoCollection<SyncLock> collection)
    {
        var @lock = collection.FindOneAndUpdate(
            Builders<SyncLock>.Filter.Eq(c => c.Id, 1),
            Builders<SyncLock>.Update
                .Set(c => c.Id, 1)
                .Set(c => c.IsRequested, true),
            new FindOneAndUpdateOptions<SyncLock>()
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.Before
            }
        );
        if (@lock is null)
        {
            return new SyncLock()
            {
                LastPage = 0,
                IsRequested = false
            };
        }
        return @lock;
    }


    // Return p
    public static SyncLock ResetPageCount(this IMongoCollection<SyncLock> collection)
    {
        var @lock = collection.FindOneAndUpdate(
            Builders<SyncLock>.Filter.Eq(c => c.Id, 1),
            Builders<SyncLock>.Update
                .Set(c => c.LastPage, 0)
                .Set(c => c.IsRequested, true),
            new FindOneAndUpdateOptions<SyncLock>()
            {
                ReturnDocument = ReturnDocument.After
            }
        );

        return @lock;
    }

    // Return p
    public static SyncLock Finish(this IMongoCollection<SyncLock> collection)
    {
        var @lock = collection.FindOneAndUpdate(
            Builders<SyncLock>.Filter.Eq(c => c.Id, 1),
            Builders<SyncLock>.Update
                .Set(c => c.IsRequested, false),
            new FindOneAndUpdateOptions<SyncLock>()
            {
                ReturnDocument = ReturnDocument.After
            }
        );

        return @lock;
    }

    // Return p
    public static SyncLock IncShowPage(this IMongoCollection<SyncLock> collection)
    {
        var @lock = collection.FindOneAndUpdate(
            Builders<SyncLock>.Filter.Eq(c => c.Id, 1),
            Builders<SyncLock>.Update
                .Inc(c => c.LastPage, 1),
            new FindOneAndUpdateOptions<SyncLock>()
            {
                ReturnDocument = ReturnDocument.After
            }
        );

        return @lock;
    }

}