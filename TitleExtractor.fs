module TitleExtractor

    open Microsoft.Extensions.AI
    open Domain
    open System

    type TitleExtractorConfig = {
        ChatClient: IChatClient
        NodesPerTitle: int
        TitlePrompt: string
    }

    let createTitleExtractor (config: TitleExtractorConfig) : Transformation =
        
        let groupNodes nodes =
            nodes |> List.chunkBySize config.NodesPerTitle
        
        let extractContextFromGroup (nodeGroup: BaseNode list) =
            nodeGroup
            |> List.map (fun node -> node.Text)
            |> String.concat "\n\n"
        
        let callChatClientForTitle context =
            async {
                try
                    let prompt = config.TitlePrompt.Replace("{context_str}", context)
                    let messages = [
                        ChatMessage(ChatRole.User, prompt)
                    ]
                    
                    let! response = config.ChatClient.GetResponseAsync(messages) |> Async.AwaitTask
                    
                    return 
                        Some response.Text
                        |> Option.defaultValue ""
                        |> fun (title:string) -> 
                            if title.StartsWith("Title:", StringComparison.OrdinalIgnoreCase)
                            then title.Substring(6).Trim()
                            else title.Trim()
                with
                | ex -> 
                    printfn $"Error extracting title: {ex.Message}"
                    return ""
            }
        
        let extractTitlesForGroups nodeGroups =
            async {
                let! titles = 
                    nodeGroups
                    |> List.map extractContextFromGroup
                    |> List.map callChatClientForTitle
                    |> Async.Parallel
                
                return titles |> Array.toList
            }
        
        let applyTitlesToNodes (titles: string list) (nodes: BaseNode list) =
            nodes
            |> List.mapi (fun i node ->
                let titleIndex = i / config.NodesPerTitle
                let title = 
                    if titleIndex < titles.Length
                    then titles.[titleIndex] 
                    else ""
                
                { node with 
                    Metadata = node.Metadata |> Map.add "document_title" title }
            )
        
        // Main async transformation function
        fun nodes ->
            async {
                let! titles = 
                    nodes
                    |> groupNodes
                    |> extractTitlesForGroups
                
                return nodes |> applyTitlesToNodes titles
            }