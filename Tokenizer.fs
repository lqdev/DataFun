module Tokenizers

    open System
    open Microsoft.ML.Tokenizers

    // Simple whitespace tokenizer
    let simpleTokenizer (text: string) : string[] =
        text.Split([|' '; '\t'; '\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
    
    // GPT-style tokenizer (use TikToken.NET or similar)
    let gptTokenizer (text: string) : string[] =
        // In practice: use TikToken.NET
        // let encoding = Tiktoken.GetEncoding("cl100k_base")
        // encoding.Encode(text) |> Array.map string
        let encoding = TiktokenTokenizer.CreateForEncoding "cl100k_base"

        encoding.EncodeToTokens text 
        |> fst
        |> Seq.map (fun (token: EncodedToken) -> token.Value)
        |> Seq.toArray

        // Simplified approximation
        // text.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
        // |> Array.collect (fun word ->
        //     if word.Length <= 4 then [|word|]
        //     else 
        //         let numTokens = (word.Length + 3) / 4
        //         Array.init numTokens (fun i -> 
        //             let start = i * 4
        //             let length = min 4 (word.Length - start)
        //             word.Substring(start, length)
        //         )
        // )
    
    // Sentence tokenizer using regex
    let regexSentenceTokenizer (text: string) : string[] =
        let sentencePattern = @"[.!?]+\s+"
        System.Text.RegularExpressions.Regex.Split(text, sentencePattern)
        |> Array.filter (String.IsNullOrWhiteSpace >> not)