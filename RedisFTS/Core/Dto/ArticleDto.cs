namespace RedisFTS.Core.Dto
{
    internal class ArticleDto
    {
        public string Name { get; }

        public string Body { get; }

        public ArticleDto(string name, string body)
        {
            Name = name;
            Body = body;
        }
    }
}
