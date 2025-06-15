using System.Text.Json;
using System.Text;

namespace Querim.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiKey = _configuration["GeminiApiKey"] ?? throw new ArgumentNullException("GeminiApiKey is not configured");
        }

        public async Task<List<QuizQuestion>> GenerateQuestionsAsync(string text)
        {
            try
            {
                var endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
                var prompt = $@"You are an expert content creator tasked with generating exactly one high-quality multiple-choice question per slide from the provided slide texts extracted from a PDF.

Requirements:
1. Generate one question per slide, identified by a unique slide number as `id`.
2. Each question must be directly answerable only using the corresponding slide's text.
3. Questions should focus on key points, definitions, or important facts from that slide.
4. Provide exactly four answer choices per question.
5. All incorrect options (distractors) must be plausible but definitively incorrect based on that slide's content.
6. Format the output strictly as a JSON object in the following structure, without additional explanations or commentary:

{{
  ""questions"": [
    {{
      ""id"": 1,
      ""question"": ""Your question here"",
      ""answers"": [""Option 1"", ""Option 2"", ""Option 3"", ""Option 4""],
      ""correct_answer"": ""Correct Option""
    }}
    // Include one question object per slide
  ]
}}

Slide Text:
{text}

Please generate the JSON now.";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topP = 1,
                        topK = 40,
                        maxOutputTokens = 2048
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{endpoint}?key={_apiKey}", content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                return ParseGeminiResponse(responseString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling Gemini API: {ex.Message}");
                throw;
            }
        }

        public List<QuizQuestion> ParseGeminiResponse(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // Handle different response formats
                if (root.TryGetProperty("candidates", out var candidates) &&
                    candidates.ValueKind == JsonValueKind.Array)
                {
                    foreach (var candidate in candidates.EnumerateArray())
                    {
                        if (candidate.TryGetProperty("content", out var content) &&
                            content.TryGetProperty("parts", out var parts))
                        {
                            foreach (var part in parts.EnumerateArray())
                            {
                                if (part.TryGetProperty("text", out var textElement))
                                {
                                    var text = textElement.GetString();
                                    if (!string.IsNullOrEmpty(text))
                                    {
                                        return ExtractQuestionsFromText(text);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (root.TryGetProperty("questions", out var questions))
                {
                    // Direct questions array case
                    return ParseQuestionsArray(questions);
                }

                throw new JsonException("Unable to find questions in the response");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Gemini response: {ex.Message}");
                throw;
            }
        }

        private List<QuizQuestion> ExtractQuestionsFromText(string text)
        {
            try
            {
                var jsonStart = text.IndexOf('{');
                var jsonEnd = text.LastIndexOf('}') + 1;
                if (jsonStart < 0 || jsonEnd <= jsonStart)
                {
                    throw new JsonException("Invalid JSON format in response text");
                }

                var jsonContent = text.Substring(jsonStart, jsonEnd - jsonStart);
                using var doc = JsonDocument.Parse(jsonContent);

                if (doc.RootElement.TryGetProperty("questions", out var questions))
                {
                    return ParseQuestionsArray(questions);
                }

                throw new JsonException("Questions array not found in JSON");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting questions from text: {ex.Message}");
                throw;
            }
        }

        private List<QuizQuestion> ParseQuestionsArray(JsonElement questionsArray)
        {
            var questions = new List<QuizQuestion>();

            foreach (var questionElement in questionsArray.EnumerateArray())
            {
                try
                {
                    var question = new QuizQuestion
                    {
                        Id = questionElement.GetProperty("id").GetInt32(),
                        QuestionText = questionElement.GetProperty("question").GetString() ?? string.Empty,
                        CorrectAnswer = questionElement.GetProperty("correct_answer").GetString() ?? string.Empty,
                        Answers = new List<string>()
                    };

                    foreach (var answer in questionElement.GetProperty("answers").EnumerateArray())
                    {
                        question.Answers.Add(answer.GetString() ?? string.Empty);
                    }

                    questions.Add(question);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing question: {ex.Message}");
                    // Continue with next question if one fails
                }
            }

            return questions;
        }

        public class QuizQuestion
        {
            public int Id { get; set; }
            public string QuestionText { get; set; } = string.Empty;
            public List<string> Answers { get; set; } = new List<string>();
            public string CorrectAnswer { get; set; } = string.Empty;
        }
    }
}
//var prompt = $@"Generate exactly high-quality multiple choice questions on every slide on {text} as JSON structure based on the following text:
//Format should be:
//{{
//    ""questions"": [
//        {{
//            ""id"": 1,
//            ""question"": ""Your question here"",
//            ""answers"": [""Option 1"", ""Option 2"", ""Option 3"", ""Option 4""],
//            ""correct_answer"": ""Correct Option""
//        }}
//        // More questions...
//    ]
//}}
//Text content: {text}";

