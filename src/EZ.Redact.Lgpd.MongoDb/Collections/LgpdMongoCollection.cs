using System.Linq.Expressions;
using EZ.Redact.Lgpd.Core;
using EZ.Redact.Lgpd.MongoDb.Internal;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Search;

namespace EZ.Redact.Lgpd.MongoDb.Collections;

internal sealed class LgpdCursor<T> : IAsyncCursor<T>
{
    private readonly IAsyncCursor<T> _inner;
    private readonly ILGPDRedactService _redactService;
    private bool _redacted;

    public LgpdCursor(IAsyncCursor<T> inner, ILGPDRedactService redactService)
    {
        _inner = inner;
        _redactService = redactService;
    }

    public IEnumerable<T> Current => _inner.Current;

    public bool MoveNext(CancellationToken cancellationToken = default)
    {
        var result = _inner.MoveNext(cancellationToken);
        if (result && !_redacted)
        {
            ApplyRedaction();
            _redacted = true;
        }
        return result;
    }

    public async Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        var result = await _inner.MoveNextAsync(cancellationToken).ConfigureAwait(false);
        if (result && !_redacted)
        {
            ApplyRedaction();
            _redacted = true;
        }
        return result;
    }

    private void ApplyRedaction()
    {
        foreach (var item in _inner.Current)
        {
            RedactionHelper.Redact(item, _redactService);
        }
    }

    public void Dispose() => _inner.Dispose();
}

internal sealed class LgpdFindFluent<T> : IFindFluent<T, T>
{
    private readonly IFindFluent<T, T> _inner;
    private readonly ILGPDRedactService _redactService;

    public LgpdFindFluent(IFindFluent<T, T> inner, ILGPDRedactService redactService)
    {
        _inner = inner;
        _redactService = redactService;
    }

    public FilterDefinition<T> Filter
    {
        get => _inner.Filter;
        set => _inner.Filter = value;
    }

    public FindOptions<T, T> Options => _inner.Options;

    public IFindFluent<T, T> Limit(int? limit) =>
        new LgpdFindFluent<T>(_inner.Limit(limit), _redactService);

    public IFindFluent<T, T> Skip(int? skip) =>
        new LgpdFindFluent<T>(_inner.Skip(skip), _redactService);

    public IFindFluent<T, T> Sort(SortDefinition<T> sort) =>
        new LgpdFindFluent<T>(_inner.Sort(sort), _redactService);

    public IFindFluent<T, TNewProjection> Project<TNewProjection>(
        ProjectionDefinition<T, TNewProjection> projection) =>
        new LgpdFindFluent<T, TNewProjection>(
            _inner.Project(projection), _redactService);

    public IFindFluent<T, TNewProjection> As<TNewProjection>(
        IBsonSerializer<TNewProjection> resultSerializer) =>
        new LgpdFindFluent<T, TNewProjection>(
            _inner.As(resultSerializer), _redactService);

    public long Count(CancellationToken cancellationToken = default) =>
        _inner.Count(cancellationToken);

    public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
        _inner.CountAsync(cancellationToken);

    public long CountDocuments(CancellationToken cancellationToken = default) =>
        _inner.CountDocuments(cancellationToken);

    public Task<long> CountDocumentsAsync(CancellationToken cancellationToken = default) =>
        _inner.CountDocumentsAsync(cancellationToken);

    public IAsyncCursor<T> ToCursor(CancellationToken cancellationToken = default) =>
        new LgpdCursor<T>(_inner.ToCursor(cancellationToken), _redactService);

    public async Task<IAsyncCursor<T>> ToCursorAsync(CancellationToken cancellationToken = default)
    {
        var cursor = await _inner.ToCursorAsync(cancellationToken).ConfigureAwait(false);
        return new LgpdCursor<T>(cursor, _redactService);
    }

    public override string ToString() => _inner.ToString()!;

    public string ToString(ExpressionTranslationOptions translationOptions) =>
        _inner.ToString(translationOptions);
}

internal sealed class LgpdFindFluent<TDocument, TProjection> : IFindFluent<TDocument, TProjection>
{
    private readonly IFindFluent<TDocument, TProjection> _inner;
    private readonly ILGPDRedactService _redactService;

    public LgpdFindFluent(IFindFluent<TDocument, TProjection> inner, ILGPDRedactService redactService)
    {
        _inner = inner;
        _redactService = redactService;
    }

    public FilterDefinition<TDocument> Filter
    {
        get => _inner.Filter;
        set => _inner.Filter = value;
    }

    public FindOptions<TDocument, TProjection> Options => _inner.Options;

    public IFindFluent<TDocument, TProjection> Limit(int? limit) =>
        new LgpdFindFluent<TDocument, TProjection>(_inner.Limit(limit), _redactService);

    public IFindFluent<TDocument, TProjection> Skip(int? skip) =>
        new LgpdFindFluent<TDocument, TProjection>(_inner.Skip(skip), _redactService);

    public IFindFluent<TDocument, TProjection> Sort(SortDefinition<TDocument> sort) =>
        new LgpdFindFluent<TDocument, TProjection>(_inner.Sort(sort), _redactService);

    public IFindFluent<TDocument, TNewProjection> Project<TNewProjection>(
        ProjectionDefinition<TDocument, TNewProjection> projection) =>
        new LgpdFindFluent<TDocument, TNewProjection>(_inner.Project(projection), _redactService);

    public IFindFluent<TDocument, TNewProjection> As<TNewProjection>(
        IBsonSerializer<TNewProjection> resultSerializer) =>
        new LgpdFindFluent<TDocument, TNewProjection>(_inner.As(resultSerializer), _redactService);

    public long Count(CancellationToken cancellationToken = default) =>
        _inner.Count(cancellationToken);

    public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
        _inner.CountAsync(cancellationToken);

    public long CountDocuments(CancellationToken cancellationToken = default) =>
        _inner.CountDocuments(cancellationToken);

    public Task<long> CountDocumentsAsync(CancellationToken cancellationToken = default) =>
        _inner.CountDocumentsAsync(cancellationToken);

    public IAsyncCursor<TProjection> ToCursor(CancellationToken cancellationToken = default)
    {
        var cursor = _inner.ToCursor(cancellationToken);
        return WrapCursor(cursor);
    }

    public async Task<IAsyncCursor<TProjection>> ToCursorAsync(CancellationToken cancellationToken = default)
    {
        var cursor = await _inner.ToCursorAsync(cancellationToken).ConfigureAwait(false);
        return WrapCursor(cursor);
    }

    private IAsyncCursor<TProjection> WrapCursor(IAsyncCursor<TProjection> cursor)
    {
        if (typeof(TProjection) == typeof(TDocument))
            return (IAsyncCursor<TProjection>)(object)new LgpdCursor<TDocument>(
                (IAsyncCursor<TDocument>)(object)cursor, _redactService);

        return cursor;
    }

    public override string ToString() => _inner.ToString()!;

    public string ToString(ExpressionTranslationOptions translationOptions) =>
        _inner.ToString(translationOptions);
}

internal sealed class LgpdMongoCollection<T> : IMongoCollection<T>
{
    private readonly IMongoCollection<T> _inner;
    private readonly ILGPDRedactService _redactService;

    public LgpdMongoCollection(IMongoCollection<T> inner, ILGPDRedactService redactService)
    {
        _inner = inner;
        _redactService = redactService;
    }

    public CollectionNamespace CollectionNamespace => _inner.CollectionNamespace;
    public IMongoDatabase Database => _inner.Database;
    public IBsonSerializer<T> DocumentSerializer => _inner.DocumentSerializer;
    public IMongoIndexManager<T> Indexes => _inner.Indexes;
    public IMongoSearchIndexManager SearchIndexes => _inner.SearchIndexes;
    public MongoCollectionSettings Settings => _inner.Settings;

    public IAsyncCursor<TResult> Aggregate<TResult>(
        PipelineDefinition<T, TResult> pipeline,
        AggregateOptions options,
        CancellationToken cancellationToken) =>
        _inner.Aggregate(pipeline, options, cancellationToken);

    public IAsyncCursor<TResult> Aggregate<TResult>(
        IClientSessionHandle session,
        PipelineDefinition<T, TResult> pipeline,
        AggregateOptions options,
        CancellationToken cancellationToken) =>
        _inner.Aggregate(session, pipeline, options, cancellationToken);

    public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(
        PipelineDefinition<T, TResult> pipeline,
        AggregateOptions options,
        CancellationToken cancellationToken) =>
        _inner.AggregateAsync(pipeline, options, cancellationToken);

    public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(
        IClientSessionHandle session,
        PipelineDefinition<T, TResult> pipeline,
        AggregateOptions options,
        CancellationToken cancellationToken) =>
        _inner.AggregateAsync(session, pipeline, options, cancellationToken);

    public void AggregateToCollection<TResult>(
        PipelineDefinition<T, TResult> pipeline,
        AggregateOptions options,
        CancellationToken cancellationToken) =>
        _inner.AggregateToCollection(pipeline, options, cancellationToken);

    public void AggregateToCollection<TResult>(
        IClientSessionHandle session,
        PipelineDefinition<T, TResult> pipeline,
        AggregateOptions options,
        CancellationToken cancellationToken) =>
        _inner.AggregateToCollection(session, pipeline, options, cancellationToken);

    public Task AggregateToCollectionAsync<TResult>(
        PipelineDefinition<T, TResult> pipeline,
        AggregateOptions options,
        CancellationToken cancellationToken) =>
        _inner.AggregateToCollectionAsync(pipeline, options, cancellationToken);

    public Task AggregateToCollectionAsync<TResult>(
        IClientSessionHandle session,
        PipelineDefinition<T, TResult> pipeline,
        AggregateOptions options,
        CancellationToken cancellationToken) =>
        _inner.AggregateToCollectionAsync(session, pipeline, options, cancellationToken);

    public BulkWriteResult<T> BulkWrite(
        IEnumerable<WriteModel<T>> requests,
        BulkWriteOptions options,
        CancellationToken cancellationToken) =>
        _inner.BulkWrite(requests, options, cancellationToken);

    public BulkWriteResult<T> BulkWrite(
        IClientSessionHandle session,
        IEnumerable<WriteModel<T>> requests,
        BulkWriteOptions options,
        CancellationToken cancellationToken) =>
        _inner.BulkWrite(session, requests, options, cancellationToken);

    public Task<BulkWriteResult<T>> BulkWriteAsync(
        IEnumerable<WriteModel<T>> requests,
        BulkWriteOptions options,
        CancellationToken cancellationToken) =>
        _inner.BulkWriteAsync(requests, options, cancellationToken);

    public Task<BulkWriteResult<T>> BulkWriteAsync(
        IClientSessionHandle session,
        IEnumerable<WriteModel<T>> requests,
        BulkWriteOptions options,
        CancellationToken cancellationToken) =>
        _inner.BulkWriteAsync(session, requests, options, cancellationToken);

    public long Count(
        FilterDefinition<T> filter,
        CountOptions options,
        CancellationToken cancellationToken) =>
        _inner.Count(filter, options, cancellationToken);

    public long Count(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        CountOptions options,
        CancellationToken cancellationToken) =>
        _inner.Count(session, filter, options, cancellationToken);

    public Task<long> CountAsync(
        FilterDefinition<T> filter,
        CountOptions options,
        CancellationToken cancellationToken) =>
        _inner.CountAsync(filter, options, cancellationToken);

    public Task<long> CountAsync(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        CountOptions options,
        CancellationToken cancellationToken) =>
        _inner.CountAsync(session, filter, options, cancellationToken);

    public long CountDocuments(
        FilterDefinition<T> filter,
        CountOptions options,
        CancellationToken cancellationToken) =>
        _inner.CountDocuments(filter, options, cancellationToken);

    public long CountDocuments(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        CountOptions options,
        CancellationToken cancellationToken) =>
        _inner.CountDocuments(session, filter, options, cancellationToken);

    public Task<long> CountDocumentsAsync(
        FilterDefinition<T> filter,
        CountOptions options,
        CancellationToken cancellationToken) =>
        _inner.CountDocumentsAsync(filter, options, cancellationToken);

    public Task<long> CountDocumentsAsync(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        CountOptions options,
        CancellationToken cancellationToken) =>
        _inner.CountDocumentsAsync(session, filter, options, cancellationToken);

    public DeleteResult DeleteMany(
        FilterDefinition<T> filter,
        CancellationToken cancellationToken) =>
        _inner.DeleteMany(filter, cancellationToken);

    public DeleteResult DeleteMany(
        FilterDefinition<T> filter,
        DeleteOptions options,
        CancellationToken cancellationToken) =>
        _inner.DeleteMany(filter, options, cancellationToken);

    public DeleteResult DeleteMany(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        DeleteOptions options,
        CancellationToken cancellationToken) =>
        _inner.DeleteMany(session, filter, options, cancellationToken);

    public Task<DeleteResult> DeleteManyAsync(
        FilterDefinition<T> filter,
        CancellationToken cancellationToken) =>
        _inner.DeleteManyAsync(filter, cancellationToken);

    public Task<DeleteResult> DeleteManyAsync(
        FilterDefinition<T> filter,
        DeleteOptions options,
        CancellationToken cancellationToken) =>
        _inner.DeleteManyAsync(filter, options, cancellationToken);

    public Task<DeleteResult> DeleteManyAsync(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        DeleteOptions options,
        CancellationToken cancellationToken) =>
        _inner.DeleteManyAsync(session, filter, options, cancellationToken);

    public DeleteResult DeleteOne(
        FilterDefinition<T> filter,
        CancellationToken cancellationToken) =>
        _inner.DeleteOne(filter, cancellationToken);

    public DeleteResult DeleteOne(
        FilterDefinition<T> filter,
        DeleteOptions options,
        CancellationToken cancellationToken) =>
        _inner.DeleteOne(filter, options, cancellationToken);

    public DeleteResult DeleteOne(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        DeleteOptions options,
        CancellationToken cancellationToken) =>
        _inner.DeleteOne(session, filter, options, cancellationToken);

    public Task<DeleteResult> DeleteOneAsync(
        FilterDefinition<T> filter,
        CancellationToken cancellationToken) =>
        _inner.DeleteOneAsync(filter, cancellationToken);

    public Task<DeleteResult> DeleteOneAsync(
        FilterDefinition<T> filter,
        DeleteOptions options,
        CancellationToken cancellationToken) =>
        _inner.DeleteOneAsync(filter, options, cancellationToken);

    public Task<DeleteResult> DeleteOneAsync(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        DeleteOptions options,
        CancellationToken cancellationToken) =>
        _inner.DeleteOneAsync(session, filter, options, cancellationToken);

    public IAsyncCursor<TField> Distinct<TField>(
        FieldDefinition<T, TField> field,
        FilterDefinition<T> filter,
        DistinctOptions options,
        CancellationToken cancellationToken) =>
        _inner.Distinct(field, filter, options, cancellationToken);

    public IAsyncCursor<TField> Distinct<TField>(
        IClientSessionHandle session,
        FieldDefinition<T, TField> field,
        FilterDefinition<T> filter,
        DistinctOptions options,
        CancellationToken cancellationToken) =>
        _inner.Distinct(session, field, filter, options, cancellationToken);

    public Task<IAsyncCursor<TField>> DistinctAsync<TField>(
        FieldDefinition<T, TField> field,
        FilterDefinition<T> filter,
        DistinctOptions options,
        CancellationToken cancellationToken) =>
        _inner.DistinctAsync(field, filter, options, cancellationToken);

    public Task<IAsyncCursor<TField>> DistinctAsync<TField>(
        IClientSessionHandle session,
        FieldDefinition<T, TField> field,
        FilterDefinition<T> filter,
        DistinctOptions options,
        CancellationToken cancellationToken) =>
        _inner.DistinctAsync(session, field, filter, options, cancellationToken);

    public IAsyncCursor<TItem> DistinctMany<TItem>(
        FieldDefinition<T, IEnumerable<TItem>> field,
        FilterDefinition<T> filter,
        DistinctOptions options,
        CancellationToken cancellationToken) =>
        _inner.DistinctMany(field, filter, options, cancellationToken);

    public IAsyncCursor<TItem> DistinctMany<TItem>(
        IClientSessionHandle session,
        FieldDefinition<T, IEnumerable<TItem>> field,
        FilterDefinition<T> filter,
        DistinctOptions options,
        CancellationToken cancellationToken) =>
        _inner.DistinctMany(session, field, filter, options, cancellationToken);

    public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(
        FieldDefinition<T, IEnumerable<TItem>> field,
        FilterDefinition<T> filter,
        DistinctOptions options,
        CancellationToken cancellationToken) =>
        _inner.DistinctManyAsync(field, filter, options, cancellationToken);

    public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(
        IClientSessionHandle session,
        FieldDefinition<T, IEnumerable<TItem>> field,
        FilterDefinition<T> filter,
        DistinctOptions options,
        CancellationToken cancellationToken) =>
        _inner.DistinctManyAsync(session, field, filter, options, cancellationToken);

    public long EstimatedDocumentCount(
        EstimatedDocumentCountOptions options,
        CancellationToken cancellationToken) =>
        _inner.EstimatedDocumentCount(options, cancellationToken);

    public Task<long> EstimatedDocumentCountAsync(
        EstimatedDocumentCountOptions options,
        CancellationToken cancellationToken) =>
        _inner.EstimatedDocumentCountAsync(options, cancellationToken);

    // Shadow the Find extension method with instance methods

    public IFindFluent<T, T> Find(
        Expression<Func<T, bool>> filter,
        FindOptions? options = null)
    {
        var innerResult = ((IMongoCollection<T>)_inner).Find(filter, options);
        return new LgpdFindFluent<T>(innerResult, _redactService);
    }

    public IFindFluent<T, T> Find(
        FilterDefinition<T> filter,
        FindOptions? options = null)
    {
        var innerResult = ((IMongoCollection<T>)_inner).Find(filter, options);
        return new LgpdFindFluent<T>(innerResult, _redactService);
    }

    public IFindFluent<T, T> Find(
        IClientSessionHandle session,
        Expression<Func<T, bool>> filter,
        FindOptions? options = null)
    {
        var innerResult = ((IMongoCollection<T>)_inner).Find(session, filter, options);
        return new LgpdFindFluent<T>(innerResult, _redactService);
    }

    public IFindFluent<T, T> Find(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        FindOptions? options = null)
    {
        var innerResult = ((IMongoCollection<T>)_inner).Find(session, filter, options);
        return new LgpdFindFluent<T>(innerResult, _redactService);
    }

    public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(
        FilterDefinition<T> filter,
        FindOptions<T, TProjection> options,
        CancellationToken cancellationToken)
    {
        return WrapFindAsync(_inner.FindAsync<TProjection>(filter, options, cancellationToken));
    }

    public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        FindOptions<T, TProjection> options,
        CancellationToken cancellationToken)
    {
        return WrapFindAsync(_inner.FindAsync<TProjection>(session, filter, options, cancellationToken));
    }

    public TProjection FindOneAndDelete<TProjection>(
        FilterDefinition<T> filter,
        FindOneAndDeleteOptions<T, TProjection> options,
        CancellationToken cancellationToken) =>
        _inner.FindOneAndDelete(filter, options, cancellationToken);

    public TProjection FindOneAndDelete<TProjection>(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        FindOneAndDeleteOptions<T, TProjection> options,
        CancellationToken cancellationToken) =>
        _inner.FindOneAndDelete(session, filter, options, cancellationToken);

    public Task<TProjection> FindOneAndDeleteAsync<TProjection>(
        FilterDefinition<T> filter,
        FindOneAndDeleteOptions<T, TProjection> options,
        CancellationToken cancellationToken) =>
        _inner.FindOneAndDeleteAsync(filter, options, cancellationToken);

    public Task<TProjection> FindOneAndDeleteAsync<TProjection>(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        FindOneAndDeleteOptions<T, TProjection> options,
        CancellationToken cancellationToken) =>
        _inner.FindOneAndDeleteAsync(session, filter, options, cancellationToken);

    public TProjection FindOneAndReplace<TProjection>(
        FilterDefinition<T> filter,
        T replacement,
        FindOneAndReplaceOptions<T, TProjection> options,
        CancellationToken cancellationToken) =>
        _inner.FindOneAndReplace(filter, replacement, options, cancellationToken);

    public TProjection FindOneAndReplace<TProjection>(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        T replacement,
        FindOneAndReplaceOptions<T, TProjection> options,
        CancellationToken cancellationToken) =>
        _inner.FindOneAndReplace(session, filter, replacement, options, cancellationToken);

    public Task<TProjection> FindOneAndReplaceAsync<TProjection>(
        FilterDefinition<T> filter,
        T replacement,
        FindOneAndReplaceOptions<T, TProjection> options,
        CancellationToken cancellationToken) =>
        _inner.FindOneAndReplaceAsync(filter, replacement, options, cancellationToken);

    public Task<TProjection> FindOneAndReplaceAsync<TProjection>(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        T replacement,
        FindOneAndReplaceOptions<T, TProjection> options,
        CancellationToken cancellationToken) =>
        _inner.FindOneAndReplaceAsync(session, filter, replacement, options, cancellationToken);

    public TProjection FindOneAndUpdate<TProjection>(
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        FindOneAndUpdateOptions<T, TProjection> options,
        CancellationToken cancellationToken) =>
        _inner.FindOneAndUpdate(filter, update, options, cancellationToken);

    public TProjection FindOneAndUpdate<TProjection>(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        FindOneAndUpdateOptions<T, TProjection> options,
        CancellationToken cancellationToken) =>
        _inner.FindOneAndUpdate(session, filter, update, options, cancellationToken);

    public Task<TProjection> FindOneAndUpdateAsync<TProjection>(
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        FindOneAndUpdateOptions<T, TProjection> options,
        CancellationToken cancellationToken) =>
        _inner.FindOneAndUpdateAsync(filter, update, options, cancellationToken);

    public Task<TProjection> FindOneAndUpdateAsync<TProjection>(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        FindOneAndUpdateOptions<T, TProjection> options,
        CancellationToken cancellationToken) =>
        _inner.FindOneAndUpdateAsync(session, filter, update, options, cancellationToken);

    public IAsyncCursor<TProjection> FindSync<TProjection>(
        FilterDefinition<T> filter,
        FindOptions<T, TProjection> options,
        CancellationToken cancellationToken)
    {
        var cursor = _inner.FindSync<TProjection>(filter, options, cancellationToken);
        return WrapCursor(cursor);
    }

    public IAsyncCursor<TProjection> FindSync<TProjection>(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        FindOptions<T, TProjection> options,
        CancellationToken cancellationToken)
    {
        var cursor = _inner.FindSync<TProjection>(session, filter, options, cancellationToken);
        return WrapCursor(cursor);
    }

    public void InsertMany(
        IEnumerable<T> documents,
        InsertManyOptions options,
        CancellationToken cancellationToken) =>
        _inner.InsertMany(documents, options, cancellationToken);

    public void InsertMany(
        IClientSessionHandle session,
        IEnumerable<T> documents,
        InsertManyOptions options,
        CancellationToken cancellationToken) =>
        _inner.InsertMany(session, documents, options, cancellationToken);

    public Task InsertManyAsync(
        IEnumerable<T> documents,
        InsertManyOptions options,
        CancellationToken cancellationToken) =>
        _inner.InsertManyAsync(documents, options, cancellationToken);

    public Task InsertManyAsync(
        IClientSessionHandle session,
        IEnumerable<T> documents,
        InsertManyOptions options,
        CancellationToken cancellationToken) =>
        _inner.InsertManyAsync(session, documents, options, cancellationToken);

    public void InsertOne(
        T document,
        InsertOneOptions options,
        CancellationToken cancellationToken) =>
        _inner.InsertOne(document, options, cancellationToken);

    public void InsertOne(
        IClientSessionHandle session,
        T document,
        InsertOneOptions options,
        CancellationToken cancellationToken) =>
        _inner.InsertOne(session, document, options, cancellationToken);

    public Task InsertOneAsync(
        T document,
        CancellationToken cancellationToken) =>
        _inner.InsertOneAsync(document, cancellationToken);

    public Task InsertOneAsync(
        T document,
        InsertOneOptions options,
        CancellationToken cancellationToken) =>
        _inner.InsertOneAsync(document, options, cancellationToken);

    public Task InsertOneAsync(
        IClientSessionHandle session,
        T document,
        InsertOneOptions options,
        CancellationToken cancellationToken) =>
        _inner.InsertOneAsync(session, document, options, cancellationToken);

    public IAsyncCursor<TResult> MapReduce<TResult>(
        BsonJavaScript map,
        BsonJavaScript reduce,
        MapReduceOptions<T, TResult> options,
        CancellationToken cancellationToken) =>
        _inner.MapReduce(map, reduce, options, cancellationToken);

    public IAsyncCursor<TResult> MapReduce<TResult>(
        IClientSessionHandle session,
        BsonJavaScript map,
        BsonJavaScript reduce,
        MapReduceOptions<T, TResult> options,
        CancellationToken cancellationToken) =>
        _inner.MapReduce(session, map, reduce, options, cancellationToken);

    public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(
        BsonJavaScript map,
        BsonJavaScript reduce,
        MapReduceOptions<T, TResult> options,
        CancellationToken cancellationToken) =>
        _inner.MapReduceAsync(map, reduce, options, cancellationToken);

    public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(
        IClientSessionHandle session,
        BsonJavaScript map,
        BsonJavaScript reduce,
        MapReduceOptions<T, TResult> options,
        CancellationToken cancellationToken) =>
        _inner.MapReduceAsync(session, map, reduce, options, cancellationToken);

    public IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>()
        where TDerivedDocument : T =>
        _inner.OfType<TDerivedDocument>();

    public ReplaceOneResult ReplaceOne(
        FilterDefinition<T> filter,
        T replacement,
        ReplaceOptions options,
        CancellationToken cancellationToken) =>
        _inner.ReplaceOne(filter, replacement, options, cancellationToken);

    public ReplaceOneResult ReplaceOne(
        FilterDefinition<T> filter,
        T replacement,
        UpdateOptions options,
        CancellationToken cancellationToken) =>
        _inner.ReplaceOne(filter, replacement, options, cancellationToken);

    public ReplaceOneResult ReplaceOne(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        T replacement,
        ReplaceOptions options,
        CancellationToken cancellationToken) =>
        _inner.ReplaceOne(session, filter, replacement, options, cancellationToken);

    public ReplaceOneResult ReplaceOne(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        T replacement,
        UpdateOptions options,
        CancellationToken cancellationToken) =>
        _inner.ReplaceOne(session, filter, replacement, options, cancellationToken);

    public Task<ReplaceOneResult> ReplaceOneAsync(
        FilterDefinition<T> filter,
        T replacement,
        ReplaceOptions options,
        CancellationToken cancellationToken) =>
        _inner.ReplaceOneAsync(filter, replacement, options, cancellationToken);

    public Task<ReplaceOneResult> ReplaceOneAsync(
        FilterDefinition<T> filter,
        T replacement,
        UpdateOptions options,
        CancellationToken cancellationToken) =>
        _inner.ReplaceOneAsync(filter, replacement, options, cancellationToken);

    public Task<ReplaceOneResult> ReplaceOneAsync(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        T replacement,
        ReplaceOptions options,
        CancellationToken cancellationToken) =>
        _inner.ReplaceOneAsync(session, filter, replacement, options, cancellationToken);

    public Task<ReplaceOneResult> ReplaceOneAsync(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        T replacement,
        UpdateOptions options,
        CancellationToken cancellationToken) =>
        _inner.ReplaceOneAsync(session, filter, replacement, options, cancellationToken);

    public UpdateResult UpdateMany(
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        UpdateOptions options,
        CancellationToken cancellationToken) =>
        _inner.UpdateMany(filter, update, options, cancellationToken);

    public UpdateResult UpdateMany(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        UpdateOptions options,
        CancellationToken cancellationToken) =>
        _inner.UpdateMany(session, filter, update, options, cancellationToken);

    public Task<UpdateResult> UpdateManyAsync(
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        UpdateOptions options,
        CancellationToken cancellationToken) =>
        _inner.UpdateManyAsync(filter, update, options, cancellationToken);

    public Task<UpdateResult> UpdateManyAsync(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        UpdateOptions options,
        CancellationToken cancellationToken) =>
        _inner.UpdateManyAsync(session, filter, update, options, cancellationToken);

    public UpdateResult UpdateOne(
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        UpdateOptions options,
        CancellationToken cancellationToken) =>
        _inner.UpdateOne(filter, update, options, cancellationToken);

    public UpdateResult UpdateOne(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        UpdateOptions options,
        CancellationToken cancellationToken) =>
        _inner.UpdateOne(session, filter, update, options, cancellationToken);

    public Task<UpdateResult> UpdateOneAsync(
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        UpdateOptions options,
        CancellationToken cancellationToken) =>
        _inner.UpdateOneAsync(filter, update, options, cancellationToken);

    public Task<UpdateResult> UpdateOneAsync(
        IClientSessionHandle session,
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        UpdateOptions options,
        CancellationToken cancellationToken) =>
        _inner.UpdateOneAsync(session, filter, update, options, cancellationToken);

    public IChangeStreamCursor<TResult> Watch<TResult>(
        PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline,
        ChangeStreamOptions options,
        CancellationToken cancellationToken) =>
        _inner.Watch(pipeline, options, cancellationToken);

    public IChangeStreamCursor<TResult> Watch<TResult>(
        IClientSessionHandle session,
        PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline,
        ChangeStreamOptions options,
        CancellationToken cancellationToken) =>
        _inner.Watch(session, pipeline, options, cancellationToken);

    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
        PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline,
        ChangeStreamOptions options,
        CancellationToken cancellationToken) =>
        _inner.WatchAsync(pipeline, options, cancellationToken);

    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
        IClientSessionHandle session,
        PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline,
        ChangeStreamOptions options,
        CancellationToken cancellationToken) =>
        _inner.WatchAsync(session, pipeline, options, cancellationToken);

    public IMongoCollection<T> WithReadConcern(ReadConcern readConcern) =>
        _inner.WithReadConcern(readConcern);

    public IMongoCollection<T> WithReadPreference(ReadPreference readPreference) =>
        _inner.WithReadPreference(readPreference);

    public IMongoCollection<T> WithWriteConcern(WriteConcern writeConcern) =>
        _inner.WithWriteConcern(writeConcern);

    private async Task<IAsyncCursor<TProjection>> WrapFindAsync<TProjection>(
        Task<IAsyncCursor<TProjection>> cursorTask)
    {
        var cursor = await cursorTask.ConfigureAwait(false);
        return WrapCursor(cursor);
    }

    private IAsyncCursor<TProjection> WrapCursor<TProjection>(IAsyncCursor<TProjection> cursor)
    {
        if (typeof(TProjection) == typeof(T))
            return (IAsyncCursor<TProjection>)(object)new LgpdCursor<T>(
                (IAsyncCursor<T>)(object)cursor, _redactService);

        return cursor;
    }
}
