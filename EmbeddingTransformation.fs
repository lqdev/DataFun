module EmbeddingTransformation

    open Microsoft.Extensions.AI
    open Domain

    let createEmbeddingTransformation (embeddingGenerator: IEmbeddingGenerator<string, Embedding<float32>>) : Transformation =
        
        let generateEmbeddingForNode (node: BaseNode) =
            async {
                try
                    let! embeddings = embeddingGenerator.GenerateAsync([node.Text]) |> Async.AwaitTask
                    let embedding = embeddings |> Seq.head
                    
                    return { node with Embedding = Some embedding.Vector }
                with
                | ex ->
                    printfn $"Error generating embedding for node {node.Id}: {ex.Message}"
                    return node
            }
        
        fun nodes ->
            async {
                let! embeddedNodes = 
                    nodes
                    |> List.map generateEmbeddingForNode
                    |> Async.Parallel
                
                return embeddedNodes |> Array.toList
            }