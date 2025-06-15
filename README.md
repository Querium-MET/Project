# Querium

Querium is an intelligent content extraction and question generation service that extracts text from PDFs (including scanned images using OCR) and generates high-quality multiple-choice questions using Google's Gemini API. It is designed to assist educators, trainers, and content creators in creating quizzes automatically from slide or document content.

## Features

- Extracts text from PDFs with support for:
  - Native text extraction from PDF pages
  - OCR (Optical Character Recognition) for scanned or image-based PDFs
- Integrates with Google's Gemini generative language model to create:
  - One high-quality multiple-choice question per slide or document section
- Supports bulk processing of multiple PDFs
- Robust error handling and logging for smooth operation

## Technology Stack

- .NET 8/9 (C#)
- iText7 PDF library for text extraction
- Tesseract OCR for image-based text recognition
- HttpClient to communicate with Gemini API
- JSON parsing with `System.Text.Json`

## Getting Started

### Prerequisites

- .NET 6 SDK or higher installed
- Tesseract language data files [download tessdata](https://github.com/tesseract-ocr/tessdata)
- Google Cloud account with access to Gemini API and API key

### Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/yourusername/Querium.git
   cd Querium
