using System;
using System.Collections.Generic;
using System.Linq;

namespace WatchedAnimeList.Helpers
{
    public static class AnimeSearch
    {
        public static List<T> Search<T>(IEnumerable<T> items, Func<T, string> getText, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return items.ToList();

            query = query.ToLowerInvariant();
            var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var scored = new List<(T item, int score)>();

            foreach (var item in items)
            {
                string text = getText(item).ToLowerInvariant();
                var textWords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                int score = WordMatchScore(queryWords, textWords);

                // fallback буквений, якщо score == 0
                if (score == 0)
                    score = LetterFallbackScore(text, query);

                scored.Add((item, score));
            }
            return scored.OrderByDescending(x => x.score).Select(x => x.item).ToList();
        }
        
        private static int WordMatchScore(string[] queryWords, string[] textWords)
        {
            int totalScore = 0;
            int qIndex = 0;
            int tIndex = 0;

            while (qIndex < queryWords.Length && tIndex < textWords.Length)
            {
                int bestWordScore = 0;
                int bestTextIndex = -1;

                // шукаємо найближче слово у тексті для поточного слова query
                for (int i = tIndex; i < Math.Min(tIndex + 2, textWords.Length); i++) // дозволяємо скіп 1 слово
                {
                    int s = LetterSequenceScore(queryWords[qIndex], textWords[i]);
                    if (s > bestWordScore)
                    {
                        bestWordScore = s;
                        bestTextIndex = i;
                    }
                }

                if (bestWordScore > 0)
                {
                    // бонус за позицію
                    totalScore += bestWordScore + Math.Max(0, 5 - bestTextIndex);
                    tIndex = bestTextIndex + 1;
                    qIndex++;
                }
                else
                {
                    // якщо не знайшли, перескакуємо 1 слово в тексті
                    tIndex++;
                    if (tIndex >= textWords.Length) break;
                }
            }

            return totalScore;
        }

        private static int LetterSequenceScore(string queryWord, string textWord)
        {
            int streak = 0;
            int score = 0;
            int mismatches = 0;

            int qLen = queryWord.Length;
            int tLen = textWord.Length;

            int i = 0, j = 0;
            while (i < qLen && j < tLen)
            {
                if (queryWord[i] == textWord[j])
                {
                    streak++;
                    score += streak;
                    i++;
                    j++;
                    mismatches = 0;
                }
                else
                {
                    mismatches++;
                    streak = 0;
                    if (mismatches >= 2)
                    {
                        // спробувати перескочити одну букву у тексті
                        j++;
                        mismatches = 0;
                    }
                    else
                    {
                        i++;
                        j++;
                    }
                }
            }

            return score;
        }

        private static int LetterFallbackScore(string text, string query)
        {
            int bestScore = 0;
            string src = text.ToLowerInvariant();
            string s = query.ToLowerInvariant();

            for (int start = 0; start < src.Length; start++)
            {
                int score = 0;
                int streak = 0;
                int qIndex = 0;
                int tIndex = start;

                while (qIndex < s.Length && tIndex < src.Length)
                {
                    if (s[qIndex] == src[tIndex])
                    {
                        streak++;
                        score += streak + Math.Max(1, 5 - tIndex); // бонус за початок
                        qIndex++;
                        tIndex++;
                    }
                    else
                    {
                        // спроба перескочити 1 букву у тексті
                        tIndex++;
                        streak = 0;
                    }
                }

                if (score > bestScore)
                    bestScore = score;
            }

            return bestScore;
        }

    }
}
