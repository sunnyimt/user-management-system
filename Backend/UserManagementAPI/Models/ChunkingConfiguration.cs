namespace UserManagementAPI.Models
{
    public class ChunkingConfiguration
    {
        public int Id { get; set; }
        public int ChunkSize { get; set; } = 500;
        public int ChunkOverlap { get; set; } = 50;
        public bool AutoIndex { get; set; } = true;
        public string SplitStrategy { get; set; } = "sentence"; // "sentence", "paragraph", "word"
        public bool PreserveLineBreaks { get; set; } = true;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public ChunkingConfiguration() { }

        public ChunkingConfiguration(int chunkSize, int chunkOverlap)
        {
            ChunkSize = chunkSize;
            ChunkOverlap = chunkOverlap;
        }
    }

    public class ChunkingStrategy
    {
        public enum Strategy
        {
            Word,        // Split by words
            Sentence,    // Split by sentences (recommended for text)
            Paragraph,   // Split by paragraphs
            CustomSize   // Fixed character size
        }

        public static List<string> ChunkText(string text, ChunkingConfiguration config)
        {
            return config.SplitStrategy switch
            {
                "word" => ChunkByWords(text, config),
                "sentence" => ChunkBySentences(text, config),
                "paragraph" => ChunkByParagraphs(text, config),
                _ => ChunkByCharacters(text, config)
            };
        }

        private static List<string> ChunkByCharacters(string text, ChunkingConfiguration config)
        {
            var chunks = new List<string>();
            var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var currentChunk = new System.Text.StringBuilder();

            foreach (var word in words)
            {
                if ((currentChunk.Length + word.Length + 1) > config.ChunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());

                    // Add overlap
                    var lastWords = currentChunk.ToString().Split(' ')
                        .TakeLast(Math.Max(1, config.ChunkOverlap / 10)).ToList();
                    currentChunk = new System.Text.StringBuilder(string.Join(" ", lastWords));
                }

                if (currentChunk.Length > 0)
                    currentChunk.Append(" ");
                currentChunk.Append(word);
            }

            if (currentChunk.Length > 0)
                chunks.Add(currentChunk.ToString().Trim());

            return chunks;
        }

        private static List<string> ChunkByWords(string text, ChunkingConfiguration config)
        {
            var chunks = new List<string>();
            var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var wordsPerChunk = Math.Max(10, config.ChunkSize / 5); // Estimate ~5 chars per word
            var overlapWords = Math.Max(1, config.ChunkOverlap / 5);

            for (int i = 0; i < words.Length; i += (wordsPerChunk - overlapWords))
            {
                var chunk = string.Join(" ", words.Skip(i).Take(wordsPerChunk));
                if (!string.IsNullOrWhiteSpace(chunk))
                {
                    chunks.Add(chunk);
                }
            }

            return chunks;
        }

        private static List<string> ChunkBySentences(string text, ChunkingConfiguration config)
        {
            var chunks = new List<string>();
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim() + ".")
                .Where(s => s.Length > 1)
                .ToList();

            var currentChunk = new System.Text.StringBuilder();

            foreach (var sentence in sentences)
            {
                if ((currentChunk.Length + sentence.Length) > config.ChunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());

                    // Add overlap - last sentence from previous chunk
                    if (config.ChunkOverlap > 0 && sentences.IndexOf(sentence) > 0)
                    {
                        var lastSentence = sentences[sentences.IndexOf(sentence) - 1];
                        currentChunk = new System.Text.StringBuilder(lastSentence);
                    }
                    else
                    {
                        currentChunk = new System.Text.StringBuilder();
                    }
                }

                if (currentChunk.Length > 0)
                    currentChunk.Append(" ");
                currentChunk.Append(sentence);
            }

            if (currentChunk.Length > 0)
                chunks.Add(currentChunk.ToString().Trim());

            return chunks;
        }

        private static List<string> ChunkByParagraphs(string text, ChunkingConfiguration config)
        {
            var chunks = new List<string>();
            var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            var currentChunk = new System.Text.StringBuilder();

            foreach (var paragraph in paragraphs)
            {
                if ((currentChunk.Length + paragraph.Length) > config.ChunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());

                    // Add overlap - last paragraph
                    if (config.ChunkOverlap > 0)
                    {
                        currentChunk = new System.Text.StringBuilder(paragraph.Substring(0,
                            Math.Min(config.ChunkOverlap, paragraph.Length)));
                    }
                    else
                    {
                        currentChunk = new System.Text.StringBuilder();
                    }
                }

                if (currentChunk.Length > 0)
                    currentChunk.Append("\n\n");
                currentChunk.Append(paragraph);
            }

            if (currentChunk.Length > 0)
                chunks.Add(currentChunk.ToString().Trim());

            return chunks;
        }
    }
}
