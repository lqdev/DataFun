# DataFun - Document Ingestion Pipeline

A modern F# application for processing and vectorizing text documents using AI-powered transformations. This project demonstrates a robust, extensible pipeline for ingesting documents, splitting them into chunks, extracting semantic titles, generating embeddings, and storing them in a vector database.

## 🏗️ Architecture Overview

The project follows a functional, pipeline-based architecture with the following key components:

### Core Components

1. **Domain Layer** (`Domain.fs`) - Defines core data types and interfaces
2. **Ingestion Pipeline** (`IngestionPipeline.fs`) - Orchestrates the entire processing workflow
3. **Transformations** - Modular processing steps:
   - **Sentence Splitter** (`SentenceSplitter.fs`) - Intelligent text chunking with overlap
   - **Title Extractor** (`TitleExtractor.fs`) - AI-powered semantic title generation
   - **Embedding Transformation** (`EmbeddingTransformation.fs`) - Vector embedding generation
4. **Tokenizer** (`Tokenizer.fs`) - Text tokenization utilities
5. **Vector Storage** - SQLite-based vector database integration

### Data Flow

```
Documents → Nodes → Chunking → Title Extraction → Embedding → Vector Store
    ↓           ↓         ↓            ↓              ↓           ↓
 Raw Text   BaseNode  Sentences   AI Titles    Embeddings   SQLite DB
```

### Pipeline Architecture

The system uses a **transformation pipeline pattern** where:
- Each transformation is a pure function: `BaseNode list -> Async<BaseNode list>`
- Transformations are composable and can run sequentially or in parallel
- The pipeline supports both synchronous and asynchronous processing
- Vector storage is integrated seamlessly at the end of the pipeline

## 🛠️ Technology Stack

- **Language**: F# (.NET 9.0)
- **AI Integration**: 
  - Microsoft.Extensions.AI.Abstractions
  - OllamaSharp (for local LLM integration)
- **Vector Database**: 
    - Microsoft.Extensions.VectorData.Abstractions
    - Microsoft.SemanticKernel.Connectors.SqliteVec (concrete implementation of Vector Data abstractions)
- **Tokenization**: Microsoft.ML.Tokenizers (with GPT tokenizer support)
- **Embedding Model**: Ollama all-minilm model
- **Chat Model**: Ollama gemma3n model

## 📋 Prerequisites

### Required Software

1. **.NET 9.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
2. **Ollama** - [Install Ollama](https://ollama.ai/)
   - Used for running local LLM models for embeddings and chat completions

### Required Models

After installing Ollama, pull the required models:

```powershell
# Embedding model (384-dimensional vectors)
ollama pull all-minilm

# Chat completion model for title extraction
ollama pull gemma3n
```

### System Requirements

- **Operating System**: Windows, macOS, or Linux
- **Memory**: 8GB+ RAM recommended (for running local LLM models)
- **Storage**: 2GB+ free space for models and vector database

## 🚀 Getting Started

### 1. Clone and Setup

```powershell
# Clone the repository (if using git)
git clone <repository-url>
cd DataFun

# Restore NuGet packages
dotnet restore
```

### 2. Start Ollama Service

```powershell
# Start Ollama server (if not already running)
ollama serve
```

The application expects Ollama to be running on `http://localhost:11434`.

### 3. Prepare Your Document

Place your text document in the project root. The current example uses `paul-graham.txt`, but you can modify the `Program.fs` file to process your own documents.

### 4. Build and Run

```powershell
# Build the project
dotnet build

# Run the application
dotnet run
```

### 5. View Results

The pipeline will:
1. Process your document through all transformations
2. Store vectors in `documents.db` (SQLite database)
3. Save detailed results to `output.txt`
4. Print a summary to the console

## 🔧 Configuration

### Pipeline Configuration

You can customize the pipeline behavior in `Program.fs`:

```fsharp
let sentenceSplitter = createSentenceSplitter {
    ChunkSize = 512              // Maximum tokens per chunk
    ChunkOverlap = 50           // Overlap between chunks
    Tokenizer = Tokenizers.gptTokenizer
    SentenceTokenizer = Tokenizers.regexSentenceTokenizer
}

let titleExtractor = createTitleExtractor {
    ChatClient = chatClient
    NodesPerTitle = 2           // Number of chunks per title
    TitlePrompt = "Context: {context_str}. Give a title that summarizes the content: "
}
```

### Parallel Processing

Enable parallel processing for large documents:

```fsharp
let parallelOpts = Some { NumWorkers = 4 }
```

### Custom Documents

Modify the document creation in `Program.fs`:

```fsharp
let documents = [
    {
        Id = Guid.NewGuid().ToString()
        Text = "Your document content here"
        Metadata = Map [
            "source", "your-file.txt"
            "author", "Author Name"
            "extension", ".txt"
            "mimeType", "text/plain"
        ]
    }
]
```

## 📊 Output and Results

### Console Output
- Progress updates during processing
- Final count of processed nodes
- Confirmation of vector storage

### File Outputs
- **`documents.db`** - SQLite vector database with embeddings
- **`output.txt`** - Detailed text output with all node information

### Vector Database Schema

The SQLite database stores vectors with this schema:
- **Id**: Unique identifier for each chunk
- **Text**: The actual text content
- **Metadata**: Document metadata (source, author, etc.)
- **Vector**: 384-dimensional embedding vector

## 🧩 Extending the Pipeline

### Adding Custom Transformations

Create a new transformation function:

```fsharp
let myCustomTransform : Transformation = 
    fun nodes ->
        async {
            // Your transformation logic here
            return processedNodes
        }
```

Add it to the pipeline configuration:

```fsharp
{
    Transformations = [sentenceSplitter; titleExtractor; myCustomTransform; embeddingTransform]
    VectorStore = vectorStore
    EmbeddingGenerator = Some embeddingGenerator
}
```

### Using Different Models

Replace the model names in `Program.fs`:

```fsharp
let chatClient = new OllamaApiClient("http://localhost:11434", "your-chat-model")
let embeddingGenerator = new OllamaApiClient("http://localhost:11434", "your-embedding-model")
```

## 🐛 Troubleshooting

### Common Issues

1. **Ollama Connection Error**
   - Ensure Ollama is running: `ollama serve`
   - Check if models are available: `ollama list`

2. **Model Not Found**
   - Pull required models: `ollama pull all-minilm` and `ollama pull gemma3n`

3. **Out of Memory**
   - Reduce `NumWorkers` in parallel options
   - Use smaller `ChunkSize` in sentence splitter

4. **SQLite Database Locked**
   - Close any other applications accessing `documents.db`
   - Delete `documents.db` to start fresh

### Performance Tips

- **For large documents**: Enable parallel processing with appropriate worker count
- **For limited memory**: Reduce chunk size and disable parallel processing
- **For faster processing**: Use smaller, faster models in Ollama

## 🔍 Understanding the Code

### Key Design Patterns

1. **Functional Pipeline**: Each transformation is a pure function that can be composed
2. **Async/Await**: All I/O operations use F# async workflows
3. **Type Safety**: Strong typing prevents runtime errors
4. **Immutable Data**: All data structures are immutable for thread safety

### Module Dependencies

```
Program.fs
├── IngestionPipeline.fs
├── Domain.fs
├── SentenceSplitter.fs
│   └── Tokenizer.fs
├── TitleExtractor.fs
└── EmbeddingTransformation.fs
```

## 📝 License

This project is provided as-is for educational and experimental purposes.

## 🤝 Contributing

This is an experimental project. Feel free to fork and modify for your own use cases!