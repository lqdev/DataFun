module IngestionPipeline
    
    open Microsoft.Extensions.AI
    open Microsoft.Extensions.VectorData
    open Domain

    // Convert documents to nodes
    let documentsToNodes (documents: Document list) : BaseNode list =
        documents
        |> List.map (fun doc -> {
            Id = doc.Id
            Text = doc.Text
            Metadata = doc.Metadata
            Embedding = None
            Relationships = Map.empty
        })
    
    // Apply transformations sequentially
    let applyTransformationsSequential (transformations: Transformation list) : BaseNode list -> Async<BaseNode list> =
        fun nodes ->
            async {
                let mutable currentNodes = nodes
                
                for transformation in transformations do
                    let! transformedNodes = transformation currentNodes
                    currentNodes <- transformedNodes
                
                return currentNodes
            }
    
    // Parallel processing using Async
    let applyTransformationsParallel (numWorkers: int) (transformations: Transformation list) : BaseNode list -> Async<BaseNode list> =
        fun nodes ->
            async {
                let batches = nodes |> List.chunkBySize (nodes.Length / numWorkers + 1)
                let sequentialTransform = applyTransformationsSequential transformations
                
                let! results = 
                    batches
                    |> List.map sequentialTransform
                    |> Async.Parallel
                
                return results |> Array.toList |> List.concat
            }
    
    // Vector store integration using Microsoft.Extensions.AI.VectorData
    let withVectorStore (vectorStore: VectorStoreCollection<string,VectorRecord> option) : BaseNode list -> Async<BaseNode list> =
        match vectorStore with
        | None -> fun nodes -> async { return nodes }
        | Some store ->
            fun nodes ->
                async {
                    // Convert nodes to vector records
                    let vectorRecords = 
                        nodes
                        |> List.choose (fun node ->
                            match node.Embedding with
                            | Some embedding -> 
                                Some {
                                    Id = node.Id
                                    Text = node.Text
                                    Metadata = node.Metadata
                                    Vector = embedding
                                }
                            | None -> None
                        )
                    
                    // Upsert to vector store
                    if not vectorRecords.IsEmpty then
                        do! store.UpsertAsync(vectorRecords) |> Async.AwaitTask |> Async.Ignore
                    
                    return nodes
                }
    
    // Main pipeline runner
    let run (config: PipelineConfig) (parallelOptions: ParallelOptions option) (documents: Document list) =
        async {
            // Convert documents to nodes
            let nodes = documentsToNodes documents
            
            // Choose transformation strategy
            let applyTransformations = 
                match parallelOptions with
                | Some opts when opts.NumWorkers > 1 -> 
                    applyTransformationsParallel opts.NumWorkers
                | _ -> 
                    applyTransformationsSequential
            
            // Apply transformations
            let! transformedNodes = applyTransformations config.Transformations nodes
            
            // Store in vector database
            let! finalNodes = withVectorStore config.VectorStore transformedNodes
            
            return finalNodes
        }