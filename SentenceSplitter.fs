module SentenceSplitter 

    open Domain
    open System

    type SentenceSplitterConfig = {
        ChunkSize: int
        ChunkOverlap: int
        Tokenizer: string -> string[]
        SentenceTokenizer: string -> string[]
    }

    let createSentenceSplitter (config: SentenceSplitterConfig) : Transformation =
        
        let countTokens text = config.Tokenizer text |> Array.length
        
        let splitBySentences text =
            text
            |> config.SentenceTokenizer
            |> Array.toList
        
        let getOverlapText text =
            let tokens = config.Tokenizer text
            if tokens.Length <= config.ChunkOverlap then text
            else
                tokens
                |> Array.skip (tokens.Length - config.ChunkOverlap)
                |> String.concat " "

        let createChunksWithOverlap (sentences: string list) =
            let rec buildChunks acc currentChunk currentTokens remaining =
                match remaining with
                | [] -> 
                    if String.IsNullOrEmpty(currentChunk) then acc
                    else currentChunk :: acc
                | sentence :: rest ->
                    let sentenceTokens = countTokens sentence
                    let newTokenCount = currentTokens + sentenceTokens
                    
                    if newTokenCount > config.ChunkSize && not (String.IsNullOrEmpty(currentChunk)) then
                        let overlapChunk = getOverlapText currentChunk
                        let newChunk = overlapChunk + sentence
                        buildChunks (currentChunk :: acc) newChunk (countTokens newChunk) rest
                    else
                        let updatedChunk = currentChunk + sentence
                        buildChunks acc updatedChunk newTokenCount rest
            
            buildChunks [] "" 0 sentences |> List.rev
        
        let splitNode (node: BaseNode) =
            if countTokens node.Text <= config.ChunkSize then
                [node]
            else
                node.Text
                |> splitBySentences
                |> createChunksWithOverlap
                |> List.mapi (fun i chunk -> {
                    Id = $"{node.Id}_chunk_{i}"
                    Text = chunk
                    Metadata = node.Metadata
                    Embedding = None
                    Relationships = node.Relationships
                })
        
        // Return async transformation
        fun nodes -> 
            async {
                return nodes |> List.collect splitNode
            }