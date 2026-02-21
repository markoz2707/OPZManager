namespace OPZManager.API.Services.Embeddings
{
    public static class TextChunker
    {
        public static List<string> ChunkText(string text, int chunkSize = 500, int chunkOverlap = 50)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var chunks = new List<string>();
            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return chunks;

            var currentChunk = new List<string>();
            var currentLength = 0;

            foreach (var word in words)
            {
                currentChunk.Add(word);
                currentLength += word.Length + 1; // +1 for space

                if (currentLength >= chunkSize)
                {
                    chunks.Add(string.Join(' ', currentChunk));

                    // Keep overlap words
                    var overlapWords = new List<string>();
                    var overlapLength = 0;
                    for (int i = currentChunk.Count - 1; i >= 0; i--)
                    {
                        overlapLength += currentChunk[i].Length + 1;
                        if (overlapLength > chunkOverlap)
                            break;
                        overlapWords.Insert(0, currentChunk[i]);
                    }

                    currentChunk = overlapWords;
                    currentLength = overlapLength;
                }
            }

            // Add remaining text
            if (currentChunk.Count > 0)
            {
                var remaining = string.Join(' ', currentChunk);
                // Only add if it's not a subset of the last chunk
                if (chunks.Count == 0 || remaining != chunks[^1])
                {
                    chunks.Add(remaining);
                }
            }

            return chunks;
        }

        public static int EstimateTokenCount(string text)
        {
            // Rough estimate: ~4 characters per token for English/Polish text
            return (int)Math.Ceiling(text.Length / 4.0);
        }
    }
}
