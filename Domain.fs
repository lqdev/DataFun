module Domain

    open System
    open Microsoft.Extensions.AI
    open Microsoft.Extensions.VectorData

    // Domain types
    type BaseNode = {
        [<VectorStoreKey>]
        Id: string
        [<VectorStoreData>]
        Text: string
        Metadata: Map<string, obj>

        [<VectorStoreVector(1536)>]    
        Embedding: ReadOnlyMemory<float32> option

        Relationships: Map<string, string>
    }

    type Document = {
        Id: string
        Text: string
        Metadata: Map<string, obj>
    }

    // Transformation function signature
    type Transformation = BaseNode list -> Async<BaseNode list>

    // Vector record for Microsoft.Extensions.AI.VectorData
    type VectorRecord = {
        Id: string
        Text: string
        Metadata: Map<string, obj>
        Vector: ReadOnlyMemory<float32>
    }

    // Pipeline configuration
    type PipelineConfig = {
        Transformations: Transformation list
        VectorStore: VectorStoreCollection<string, VectorRecord> option
        EmbeddingGenerator: IEmbeddingGenerator<string, Embedding<float32>> option
    }

    type ParallelOptions = {
        NumWorkers: int
    }