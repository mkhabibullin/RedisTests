using RedisFTS.Core.Dto;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisFTS.Core
{
    internal static class FTS
    {
        /// <summary>
        /// Fuzz a word to get all possible substrings of the word
        /// </summary>
        /// <param name="word">The word to fuzz</param>
        /// <param name="fuzzyLenth">Max length of fuzz words</param>
        /// <returns>The substrings of the word</returns>
        public static IEnumerable<FuzzyWordDto> GetFuzzyWords(string word, int fuzzyLenth = 3)
        {
            if (word == null || word.Length < fuzzyLenth)
            {
                return new[] { new FuzzyWordDto(word, 1) };
            }

            var words = new List<FuzzyWordDto>();

            int minWindow = fuzzyLenth;
            int maxWindow = word.Length;
            int windowCount = maxWindow - minWindow + 1;
            double weightPerWindow = 1.0 / windowCount;

            foreach(var window in Enumerable.Range(minWindow, windowCount))
            {
                var fragments = word.Length - window + 1;
                var weightPerFragment = weightPerWindow / fragments;
                foreach(var offset in Enumerable.Range(0, fragments))
                {
                    var fragment = word.Substring(offset, window);
                    words.Add(new FuzzyWordDto(fragment, weightPerFragment));
                }
            }

            return words;

    //        words = { }
    //        if len(word) < 3:
    //    words[word] = 1.0
    //    return words
    //min_window = 3
    //max_window = len(word)
    //window_count = max_window - min_window + 1
    //weight_per_window = 1.0 / window_count
    //# Same procedure as in above fuzz function.
    //# Please see video for explanation on the weights.
    //for window in range(min_window, max_window + 1):
    //    fragments = len(word) - window + 1
    //    weight_per_fragment = weight_per_window / fragments
    //    for offset in range(0, fragments):
    //        fragment = word[offset: offset + window]
    //        words[fragment] = weight_per_fragment
    //return words
        }

        public static IEnumerable<string> GetWords(string text)
        {
            return text?.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        }

        public static async Task AddArticle(RedisClient redisClient, ArticleDto article)
        {
            var words = GetWords($"{article.Name} {article.Body}");
            foreach (var word in words)
            {
                var fuzzyWords = GetFuzzyWords(word);
                foreach (var fw in fuzzyWords)
                {
                    await redisClient.Add(article.Name, fw.Word, fw.Weight);
                }
            }
        }

        public static async Task<IEnumerable<SortedSetEntry>> Search(RedisClient redisClient, string query, int limit = 5)
        {
            var result = new List<SortedSetEntry>();

            var queryWords = GetWords(query);

            if (queryWords == null || queryWords.Count() == 0)
            {
                return new SortedSetEntry[] { };
            }

            foreach(var word in queryWords)
            {
                var fuzzyWords = GetFuzzyWords(word);

                var intermidiateResult = await redisClient.GetRange(fuzzyWords.Select(fw => fw.Word).ToArray());

                result = result.Union(intermidiateResult).ToList();
            }

            return result;
        }


//# Search for a query in Redis
//        def search(r: StrictRedis, query: str, limit= 5):
//    query_words = list(get_words(query))

//    if len(query_words) == 0:
//        return []

//        keys = {}
//    for word in query_words:
//        for fuzzed_word, weight in weighted_fuzz(word).items() :
//            key = "words:{}".format(fuzzed_word)
//            keys[key] = weight

//# We use a "random" key, because Redis does not let us extract the results immediately.
//# An UUID is a generated pseudo-unique string, which should minimize key clashes.
//    destination_key = str(uuid.uuid1())
//    # Create the results
//    r.zunionstore(destination_key, keys)
//# Extract the results
//    results = r.zrevrange(destination_key, 0, limit - 1, withscores = True)
//    # Delete the results from Redis
//    r.delete(destination_key)

//    return results

    }
}
