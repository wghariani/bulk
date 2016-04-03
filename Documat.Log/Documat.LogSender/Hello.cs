using ServiceStack;

namespace Documat.LogSender
{
    [Route("/hello/{Name}")]
    public class Hello : IReturn<string>
    {
        public string Name { get; set; }
    }
}
