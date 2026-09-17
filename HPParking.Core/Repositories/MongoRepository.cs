using HPParking.Core.Data;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Common;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Core.Repositories
{
    /// <summary>
    /// Triển khai MongoDB Repository tổng quát cho toàn bộ Entities kế thừa BaseEntity
    /// Tích hợp sẵn cơ chế Xóa mềm (Soft Delete), Auto Timestamps, và ánh xạ an toàn
    /// </summary>
    public class MongoRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly MongoDbContext _context;
        protected readonly IMongoCollection<T> _collection;

        public MongoRepository(MongoDbContext? context = null, string? collectionName = null)
        {
            _context = context ?? MongoDbContext.Instance;
            _collection = !string.IsNullOrWhiteSpace(collectionName)
                ? _context.GetCollection<T>(collectionName!)
                : _context.GetCollection<T>();
        }

        public MongoRepository() : this(MongoDbContext.Instance) { }

        public IMongoCollection<T> Collection => _collection;

        /// <summary>
        /// Tạo bộ lọc tự động kết hợp điều kiện chưa bị xóa mềm (IsDeleted == false)
        /// </summary>
        protected FilterDefinition<T> CombineSoftDeleteFilter(FilterDefinition<T>? filter = null)
        {
            var notDeleted = Builders<T>.Filter.Eq(x => x.IsDeleted, false);
            return filter != null ? Builders<T>.Filter.And(notDeleted, filter) : notDeleted;
        }

        /// <summary>
        /// Tạo bộ lọc ID tương thích cả ObjectId và string
        /// </summary>
        protected FilterDefinition<T> BuildIdFilter(string id)
        {
            if (ObjectId.TryParse(id, out var objectId))
            {
                return Builders<T>.Filter.Or(
                    Builders<T>.Filter.Eq(x => x.Id, id),
                    Builders<T>.Filter.Eq("_id", objectId),
                    Builders<T>.Filter.Eq("_id", id)
                );
            }
            return Builders<T>.Filter.Or(
                Builders<T>.Filter.Eq(x => x.Id, id),
                Builders<T>.Filter.Eq("_id", id)
            );
        }

        public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            var idFilter = BuildIdFilter(id);
            var filter = CombineSoftDeleteFilter(idFilter);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var filter = CombineSoftDeleteFilter();
            return await _collection.Find(filter).ToListAsync(cancellationToken);
        }

        public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (predicate == null) return await GetAllAsync(cancellationToken);
            var notDeleted = Builders<T>.Filter.Eq(x => x.IsDeleted, false);
            var predicateFilter = Builders<T>.Filter.Where(predicate);
            var filter = Builders<T>.Filter.And(notDeleted, predicateFilter);
            return await _collection.Find(filter).ToListAsync(cancellationToken);
        }

        public virtual async Task<IReadOnlyList<T>> FindAsync(FilterDefinition<T> filter, SortDefinition<T>? sort = null, int skip = 0, int limit = 0, CancellationToken cancellationToken = default)
        {
            var combinedFilter = CombineSoftDeleteFilter(filter);
            var query = _collection.Find(combinedFilter);
            if (sort != null) query = query.Sort(sort);
            if (skip > 0) query = query.Skip(skip);
            if (limit > 0) query = query.Limit(limit);
            return await query.ToListAsync(cancellationToken);
        }

        public virtual async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (predicate == null) return null;
            var notDeleted = Builders<T>.Filter.Eq(x => x.IsDeleted, false);
            var predicateFilter = Builders<T>.Filter.Where(predicate);
            var filter = Builders<T>.Filter.And(notDeleted, predicateFilter);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var filter = CombineSoftDeleteFilter(Builders<T>.Filter.Where(predicate));
            return await _collection.Find(filter).AnyAsync(cancellationToken);
        }

        public virtual async Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
            {
                var filter = CombineSoftDeleteFilter();
                return await _collection.CountDocumentsAsync(filter, null, cancellationToken);
            }
            else
            {
                var notDeleted = Builders<T>.Filter.Eq(x => x.IsDeleted, false);
                var predicateFilter = Builders<T>.Filter.Where(predicate);
                var filter = Builders<T>.Filter.And(notDeleted, predicateFilter);
                return await _collection.CountDocumentsAsync(filter, null, cancellationToken);
            }
        }

        public virtual async Task<long> CountAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
        {
            var combinedFilter = CombineSoftDeleteFilter(filter);
            return await _collection.CountDocumentsAsync(combinedFilter, null, cancellationToken);
        }

        public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            entity.CreatedAt = DateTime.Now;
            entity.UpdatedAt = DateTime.Now;
            entity.IsDeleted = false;

            await _collection.InsertOneAsync(entity, null, cancellationToken);
            return entity;
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            var list = entities.ToList();
            if (list.Count == 0) return;

            foreach (var item in list)
            {
                item.CreatedAt = DateTime.Now;
                item.UpdatedAt = DateTime.Now;
                item.IsDeleted = false;
            }

            await _collection.InsertManyAsync(list, null, cancellationToken);
        }

        public virtual async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                return false;
            }

            entity.UpdatedAt = DateTime.Now;

            // Dùng ReplaceOneAsync với filter ID an toàn (hỗ trợ cả ObjectId và string)
            var idFilter = BuildIdFilter(entity.Id);
            var result = await _collection.ReplaceOneAsync(
                idFilter,
                entity,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken);

            return result.MatchedCount > 0;
        }

        public virtual async Task<bool> DeleteAsync(string id, bool softDelete = true, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            var idFilter = BuildIdFilter(id);
            if (softDelete)
            {
                var filter = CombineSoftDeleteFilter(idFilter);
                var update = Builders<T>.Update
                    .Set(x => x.IsDeleted, true)
                    .Set(x => x.DeletedAt, DateTime.Now)
                    .Set(x => x.UpdatedAt, DateTime.Now);

                var result = await _collection.UpdateOneAsync(filter, update, null, cancellationToken);
                return result.MatchedCount > 0;
            }
            else
            {
                var result = await _collection.DeleteOneAsync(idFilter, cancellationToken);
                return result.DeletedCount > 0;
            }
        }
    }
}
