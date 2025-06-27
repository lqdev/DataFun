open OllamaSharp
open System
open Domain
open System.IO
open Microsoft.Extensions.AI
open Microsoft.Extensions.VectorData
open IngestionPipeline
open SentenceSplitter
open Tokenizers
open TitleExtractor
open EmbeddingTransformation
open Microsoft.SemanticKernel.Connectors.SqliteVec

let createPipeline (chatClient: IChatClient) (embeddingGenerator: IEmbeddingGenerator<string, Embedding<float32>>) =

    let sentenceSplitter = createSentenceSplitter {
        ChunkSize = 512
        ChunkOverlap = 50
        Tokenizer = Tokenizers.gptTokenizer
        SentenceTokenizer = Tokenizers.regexSentenceTokenizer
    }
    
    let titleExtractor = createTitleExtractor {
        ChatClient = chatClient
        NodesPerTitle = 2
        TitlePrompt = "Context: {context_str}. Give a title that summarizes the content: "
    }
    
    let embeddingTransform = createEmbeddingTransformation embeddingGenerator
    
    let sqliteOptions = SqliteCollectionOptions()
    sqliteOptions.EmbeddingGenerator <- embeddingGenerator

    let (vectorStore:VectorStoreCollection<string,VectorRecord> option) = Some (new SqliteCollection<string,VectorRecord>("Data Source=documents.db", "documents", sqliteOptions))
    vectorStore
    |> Option.iter (fun vs -> vs.EnsureCollectionExistsAsync() |> ignore)

    {
        Transformations = [sentenceSplitter; titleExtractor; embeddingTransform]
        VectorStore = vectorStore
        EmbeddingGenerator = Some embeddingGenerator
    }

// Example usage
let runPipeline documents chatClient embeddingGenerator =
    async {
        let config = createPipeline chatClient embeddingGenerator
        let parallelOpts = Some { NumWorkers = 4 }
        
        let! result = IngestionPipeline.run config parallelOpts documents
        // return result

        let save = IngestionPipeline.withVectorStore config.VectorStore
        return! save result
    }


let (chatClient:IChatClient) = new OllamaApiClient("http://localhost:11434", "gemma3n")
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

// Save results to a file txt
let outputFile = "output.txt"
let outputContent = 
    result 
    |> List.map (fun node -> sprintf "Node ID: %s\nNode Text: %s\nNode Metadata: %A\nNode Embedding: %A\n" node.Id node.Text node.Metadata node.Embedding)
    |> String.concat "\n"

File.WriteAllText(outputFile, outputContent)
printfn "Results saved to %s" outputFile