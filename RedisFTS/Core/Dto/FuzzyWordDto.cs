namespace RedisFTS.Core.Dto
{
    class FuzzyWordDto
    {
        public double Weight { get; }

        public string Word { get; }

        public FuzzyWordDto(string word, double weight)
        {
            Word = word;
            Weight = weight;
        }
    }
}
