open OllamaSharp
open System
open Domain
open System.IO
open Microsoft.Extensions.AI
open IngestionPipeline
open SentenceSplitter
open Tokenizers
open TitleExtractor
open EmbeddingTransformation

let createPipeline (chatClient: IChatClient) (embeddingGenerator: IEmbeddingGenerator<string, Embedding<float32>>) =

    let sentenceSplitter = createSentenceSplitter {
        ChunkSize = 512
        ChunkOverlap = 50
        Tokenizer = Tokenizers.gptTokenizer
        SentenceTokenizer = Tokenizers.regexSentenceTokenizer
    }
    
    let titleExtractor = createTitleExtractor {
        ChatClient = chatClient
        NodesPerTitle = 5
        TitlePrompt = "Context: {context_str}. Give a title that summarizes the content: "
    }
    
    let embeddingTransform = createEmbeddingTransformation embeddingGenerator
    
    {
        Transformations = [sentenceSplitter; titleExtractor; embeddingTransform]
        VectorStore = None // Optionally set a vector store here
        EmbeddingGenerator = Some embeddingGenerator
    }

// Example usage
let runPipeline documents chatClient embeddingGenerator =
    async {
        let config = createPipeline chatClient embeddingGenerator
        let parallelOpts = Some { NumWorkers = 4 }
        
        let! result = IngestionPipeline.run config parallelOpts documents
        return result
    }


let (chatClient:IChatClient) = new OllamaApiClient("http://localhost:11434", "smollm2:135m")
let (embeddingGenerator:IEmbeddingGenerator<string,Embedding<float32>>) = new OllamaApiClient("http://localhost:11434", "all-minilm")

// let pipeline = IngestionPipeline.createPipeline chatClient embeddingGenerator

let content = File.ReadAllText "paul-graham.txt"

let (documents:Document list) = [
    {
        Id = Guid.NewGuid().ToString()
        Text = content
        Metadata = Map [
            "source", "paul-graham.txt"
            "author", "Paul Graham"
            "extension", ".txt"
            "mimeType", "text/plain"
        ]
    }
]

let result = 
    runPipeline documents chatClient embeddingGenerator
    |> Async.RunSynchronously


printfn "Pipeline completed with %d nodes." (List.length result)
// result 
// |> List.iter (fun node ->
//     printfn "Node ID: %s" node.Id
//     printfn "Node Text: %s" node.Text
//     printfn "Node Metadata: %A" node.Metadata
// )