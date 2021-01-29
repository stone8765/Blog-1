﻿using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace Meowv.Blog
{
    public class MongoDbRepositoryBase<TMongoDbContext, TEntity, TKey> : MongoDbRepository<TMongoDbContext, TEntity, TKey> where TMongoDbContext : IAbpMongoDbContext where TEntity : class, IEntity<TKey>
    {
        public MongoDbRepositoryBase(IMongoDbContextProvider<TMongoDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public new IMongoCollection<TEntity> Collection => GetCollectionAsync().Result;

        public async Task BulkInsertAsync(IEnumerable<TEntity> list)
        {
            await Collection.InsertManyAsync(list);
        }
    }

    public class MongoDbRepositoryBase<TEntity, TKey> : MongoDbRepositoryBase<MeowvBlogMongoDbContext, TEntity, TKey> where TEntity : class, IEntity<TKey>
    {
        public MongoDbRepositoryBase(IMongoDbContextProvider<MeowvBlogMongoDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }

    public class MongoDbRepositoryBase<TEntity> : MongoDbRepositoryBase<TEntity, ObjectId> where TEntity : class, IEntity<ObjectId>
    {
        public MongoDbRepositoryBase(IMongoDbContextProvider<MeowvBlogMongoDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}