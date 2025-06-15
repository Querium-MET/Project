<!-- Improved compatibility of back to top link: See: https://github.com/othneildrew/Best-README-Template/pull/73 -->
<a id="readme-top"></a>

<!--
*** Thanks for checking out the Querium README! If you have suggestions,
*** please fork the repo and create a pull request or open an issue.
*** And if you like it, please give the project a star! Thanks! 🚀
-->

<!-- PROJECT SHIELDS -->
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]
[![LinkedIn][linkedin-shield]][linkedin-url]

<br />
<div align="center">
  <a href="https://github.com/your_username/Querium">
    <img src="images/logo.png" alt="Querium Logo" width="80" height="80" />
  </a>

  <h3 align="center">Querium</h3>

  <p align="center">
    Intelligent PDF content extraction and AI-powered question generation service.
    <br />
    <a href="https://github.com/your_username/Querium"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="https://github.com/your_username/Querium">View Demo</a>
    &middot;
    <a href="https://github.com/your_username/Querium/issues/new?labels=bug&template=bug-report.md">Report Bug</a>
    &middot;
    <a href="https://github.com/your_username/Querium/issues/new?labels=enhancement&template=feature-request.md">Request Feature</a>
  </p>
</div>

---

<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#about-the-project">About The Project</a>
      <ul><li><a href="#built-with">Built With</a></li></ul>
    </li>
    <li><a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>

---

## About The Project

[![Querium Screenshot][product-screenshot]](https://github.com/your_username/Querium)

Querium is a powerful .NET-based service that extracts text content from PDFs—including scanned documents with the help of OCR—and generates high-quality multiple-choice questions using the Google Gemini API's generative AI. It is designed for educators, trainers, and content creators who want to automate quiz creation from any PDF document such as slide decks, textbooks, and reports.

**Why Querium?**

* Automates the tedious task of question creation from content
* Supports both native PDF text and image-based PDFs via OCR
* Takes advantage of cutting-edge AI (Google Gemini) for question generation
* Easily extensible for bulk PDF processing and scalable API integration

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Built With

* [.NET 9](https://dotnet.microsoft.com/)
* [iText 7 PDF Library](https://itextpdf.com/en)
* [Tesseract OCR Library](https://github.com/tesseract-ocr/tesseract)
* [Google Gemini API](https://developers.generativelanguage.google/products/gemini)
* [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/api/system.text.json)

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## Getting Started

Follow these instructions to set up Querium locally and get started.

### Prerequisites

- [.NET 7 SDK or later](https://dotnet.microsoft.com/download)
- Tesseract language data for OCR [Download tessdata](https://github.com/tesseract-ocr/tessdata)
- Google Cloud account with access to Gemini API & API key

### Installation

1. Clone the repository:

```bash
git clone https://github.com/your_username/Querium.git
cd Querium
